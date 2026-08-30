using AethericForge.Runtime.Abstractions.Interfaces.Identity.Lifecycle;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Services;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Subjects;
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Campus;
using AethericForge.Runtime.Institutions.Registry;
using AethericForge.Runtime.Models.Authorities;
using AethericForge.Runtime.Providers.Identity.Keycloak;
using AethericForge.Runtime.Services.Identity;
using AethericForge.Runtime.Services.Identity.Lifecycle;
using AethericForge.Runtime.Services.Registry;
using System.Security.Claims;

namespace AethericGm.Web.Composition;

public sealed class AethericGmCampus : IHostedService
{
    public AethericGmCampus(IServiceProvider services, KeycloakIdentityProvider provider)
    {
        var campusTemplate = InstitutionTemplateBuilder.Create().UseModule<CampusModule>().Build();
        Campus = new Campus(new CampusContext(campusTemplate, services));

        var lifecycle = new IdentityLifecycleService(Array.Empty<IIdentityLifecyclePolicy>());
        var identity = new IdentityService([provider], lifecycle);
        var registrar = new Registrar(identity);
        var registryService = new RegistryService(identity, new Team<IRegistryClerk>([]));

        var registryTemplate = InstitutionTemplateBuilder.Create()
            .WithDescriptor("Aetheric GM Registry", new Version(1, 0, 0), "Local identity registry for Aetheric GM.")
            .Build();
        Registry = new Registry(new RegistryContext(registryTemplate, services, Campus), registryService, registrar);
        Campus.Register<IRegistry>(Registry);
    }

    public Campus Campus { get; }
    public Registry Registry { get; }

    public Task<IIdentitySubject?> ResolveOperatorAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var subjectId = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(subjectId)
            ? Task.FromResult<IIdentitySubject?>(null)
            : Registry.Registrar.ResolveSubjectAsync(
                AethericForge.Runtime.Abstractions.Interfaces.Identity.Authentication.IdentityScheme.OpenIdConnect,
                subjectId,
                cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Campus.InitializeAsync(cancellationToken);
        await Registry.InitializeAsync(cancellationToken);
        await Campus.StartAsync(cancellationToken);
        await Registry.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Registry.StopAsync(cancellationToken);
        await Campus.StopAsync(cancellationToken);
    }
}
