using AethericGm.Web.Components;
using AethericGm.Web.Composition;
using AethericGm.Core.Profiles;
using AethericGm.Infrastructure.Composition;
using AethericGm.Institutions.Gm;
using AethericGm.Web.Profiles;
using AethericGm.Web.Dice;
using AethericForge.Runtime.Providers.Identity.Keycloak;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-AethericGm";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/account/login";
    })
    .AddOpenIdConnect(options =>
    {
        var keycloak = builder.Configuration.GetRequiredSection("Keycloak");
        options.Authority = keycloak["Authority"];
        options.ClientId = keycloak["ClientId"];
        options.ClientSecret = keycloak["ClientSecret"];
        options.ResponseType = "code";
        options.UsePkce = true;
        options.MapInboundClaims = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped(sp => new DiceTrayState(sp.GetRequiredService<IAethericGm>().Dice));

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var protectionKeysDirectory = Path.Combine(dataDirectory, "DataProtection-Keys");
Directory.CreateDirectory(protectionKeysDirectory);
if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(protectionKeysDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(protectionKeysDirectory)).SetApplicationName("AethericGm");
builder.Services.AddSingleton<ISshPrivateKeyProtector, DataProtectionSshPrivateKeyProtector>();
var rulesCatalogPath = Path.GetFullPath(builder.Configuration["RulesCatalog:Path"] ?? "../../rulesets", builder.Environment.ContentRootPath);
builder.Services.AddLocalGmStorage(new LocalGmStorageOptions(dataDirectory, rulesCatalogPath));
builder.Services.AddSingleton(sp =>
{
    var gm = sp.GetRequiredService<IAethericGm>();
    return new AethericGm.Web.People.CampaignEntityDirectory(gm.Npcs, gm.People, gm.Characters, gm.Places);
});
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetRequiredSection("Keycloak"));
builder.Services.AddHttpClient("Keycloak");
builder.Services.AddSingleton(services => new KeycloakIdentityProvider(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("Keycloak"),
    services.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeycloakOptions>>().Value));
builder.Services.AddSingleton<AethericGmCampus>();
builder.Services.AddSingleton<IAethericGm>(sp => sp.GetRequiredService<AethericGmCampus>().Gm);
builder.Services.AddHostedService(services => services.GetRequiredService<AethericGmCampus>());

var app = builder.Build();
await app.Services.InitializeLocalGmStorageAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/account/login", (string? returnUrl) =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = LocalReturnUrl(returnUrl) },
        [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();
app.MapPost("/account/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    return Results.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .RequireAuthorization()
    .AddInteractiveServerRenderMode();

app.Run();

static string LocalReturnUrl(string? returnUrl) =>
    !string.IsNullOrWhiteSpace(returnUrl) &&
    returnUrl.StartsWith("/", StringComparison.Ordinal) &&
    !returnUrl.StartsWith("//", StringComparison.Ordinal)
        ? returnUrl
        : "/";
