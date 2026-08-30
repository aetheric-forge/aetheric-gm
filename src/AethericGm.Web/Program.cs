using AethericGm.Web.Components;
using AethericGm.Core.Campaigns;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Core.Rules;
using AethericGm.Infrastructure.Rules;
using AethericGm.Web.Composition;
using AethericForge.Runtime.Providers.Identity.Keycloak;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
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

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var campaignRepository = new SqliteCampaignRepository($"Data Source={Path.Combine(dataDirectory, "aetheric-gm.db")}");
await campaignRepository.InitializeAsync();
builder.Services.AddSingleton<ICampaignRepository>(campaignRepository);
var rulesCatalogPath = Path.GetFullPath(builder.Configuration["RulesCatalog:Path"] ?? "../../rulesets", builder.Environment.ContentRootPath);
builder.Services.AddSingleton<IRulesCatalog>(new FileRulesCatalog(rulesCatalogPath));
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetRequiredSection("Keycloak"));
builder.Services.AddHttpClient("Keycloak");
builder.Services.AddSingleton(services => new KeycloakIdentityProvider(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("Keycloak"),
    services.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeycloakOptions>>().Value));
builder.Services.AddSingleton<AethericGmCampus>();
builder.Services.AddHostedService(services => services.GetRequiredService<AethericGmCampus>());

var app = builder.Build();

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
