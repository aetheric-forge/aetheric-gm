using AethericGm.Web.Components;
using AethericGm.Core.Campaigns;
using AethericGm.Infrastructure.Campaigns;
using AethericGm.Core.Rules;
using AethericGm.Infrastructure.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Infrastructure.Rules.CharacterSheets;
using AethericGm.Web.Composition;
using AethericGm.Core.Profiles;
using AethericGm.Infrastructure.Profiles;
using AethericGm.Web.Profiles;
using AethericGm.Core.Rules.Packages;
using AethericGm.Infrastructure.Rules.Packages;
using AethericGm.Core.Dice;
using AethericGm.Web.Dice;
using AethericGm.Core.Characters;
using AethericGm.Infrastructure.Characters;
using AethericGm.Core.Npcs;
using AethericGm.Infrastructure.Npcs;
using AethericGm.Web.Rules;
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
builder.Services.AddSingleton<IDiceRandomSource, CryptographicDiceRandomSource>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDiceRoller, DiceRoller>();
builder.Services.AddScoped<DiceTrayState>();

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDirectory);
var databaseConnectionString = $"Data Source={Path.Combine(dataDirectory, "aetheric-gm.db")}";
var protectionKeysDirectory = Path.Combine(dataDirectory, "DataProtection-Keys");
Directory.CreateDirectory(protectionKeysDirectory);
if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(protectionKeysDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(protectionKeysDirectory)).SetApplicationName("AethericGm");
builder.Services.AddSingleton<ISshPrivateKeyProtector, DataProtectionSshPrivateKeyProtector>();
builder.Services.AddSingleton<ISshCredentialService>(services => new SqliteSshCredentialService(databaseConnectionString, services.GetRequiredService<ISshPrivateKeyProtector>()));
var installedPackagesPath = Path.Combine(dataDirectory, "RulesPackages");
builder.Services.AddSingleton<IRulesPackageInstaller>(services => new GitRulesPackageInstaller(
    databaseConnectionString, installedPackagesPath, services.GetRequiredService<ISshCredentialService>(),
    services.GetRequiredService<ILogger<GitRulesPackageInstaller>>()));
var campaignRepository = new SqliteCampaignRepository(databaseConnectionString);
await campaignRepository.InitializeAsync();
builder.Services.AddSingleton<ICampaignRepository>(campaignRepository);
var characterRepository = new SqliteCharacterRepository(databaseConnectionString);
await characterRepository.InitializeAsync();
builder.Services.AddSingleton<ICharacterRepository>(characterRepository);
var npcRepository = new SqliteNpcRepository(databaseConnectionString);
await npcRepository.InitializeAsync();
builder.Services.AddSingleton<INpcRepository>(npcRepository);
var rulesCatalogPath = Path.GetFullPath(builder.Configuration["RulesCatalog:Path"] ?? "../../rulesets", builder.Environment.ContentRootPath);
var rulesCatalog = new FileRulesCatalog(rulesCatalogPath);
builder.Services.AddSingleton<IRulesCatalog>(rulesCatalog);
builder.Services.AddSingleton<ICharacterSheetDefinitionStore>(new FileCharacterSheetDefinitionStore(rulesCatalogPath, rulesCatalog));
builder.Services.AddSingleton<RulesetWorkspaceResolver>();
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetRequiredSection("Keycloak"));
builder.Services.AddHttpClient("Keycloak");
builder.Services.AddSingleton(services => new KeycloakIdentityProvider(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("Keycloak"),
    services.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeycloakOptions>>().Value));
builder.Services.AddSingleton<AethericGmCampus>();
builder.Services.AddHostedService(services => services.GetRequiredService<AethericGmCampus>());

var app = builder.Build();
await app.Services.GetRequiredService<ISshCredentialService>().InitializeAsync();
await app.Services.GetRequiredService<IRulesPackageInstaller>().InitializeAsync();

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
