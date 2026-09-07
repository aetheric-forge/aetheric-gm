using AethericForge.Runtime.Institutions.Registry;
using AethericForge.Runtime.Providers.Identity.Keycloak;
using AethericGm.Web.Composition;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace AethericGm.Tests;

public sealed class AethericGmCampusTests
{
    [Fact]
    public async Task Registry_is_contained_by_campus_and_resolves_sso_operator()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var provider = new KeycloakIdentityProvider(new HttpClient(), new KeycloakOptions
        { Authority = "https://identity.example/realms/test", Realm = "test", ClientId = "aetheric-gm" });
        var host = new AethericGmCampus(services, provider);

        Assert.Null(host.Campus.Context.Parent);
        Assert.Same(host.Campus, host.Registry.Context.Parent);
        Assert.Same(host.Registry, host.Campus.Resolve<IRegistry>());

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "operator-1"), new Claim("name", "Keeper")], "oidc", "name", "role"));
        var subject = await host.ResolveOperatorAsync(principal);

        Assert.Equal("operator-1", subject?.SubjectId);
        Assert.Equal(AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication.IdentityScheme.OpenIdConnect, subject?.Scheme);
    }
}
