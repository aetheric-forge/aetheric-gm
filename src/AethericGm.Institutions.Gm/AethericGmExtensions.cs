using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Campus;
using Microsoft.Extensions.DependencyInjection;

namespace AethericGm.Institutions.Gm;

public static class AethericGmExtensions
{
    /// <summary>
    /// Registers GM beneath an existing campus. The host supplies the game services;
    /// this method does not create a campus, providers, authentication, or storage.
    /// The host owns initialization, startup, and shutdown of the returned institution.
    /// </summary>
    public static IAethericGm RegisterAethericGm(this ICampus campus, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(campus);
        ArgumentNullException.ThrowIfNull(services);
        var template = InstitutionTemplateBuilder.Create()
            .WithDescriptor("Aetheric GM", new Version(1, 0, 0), "Rules-neutral campaign and character tools.")
            .Build();
        var institution = ActivatorUtilities.CreateInstance<AethericGmInstitution>(
            services, new AethericGmContext(template, services, campus));
        campus.Register<IAethericGm>(institution);
        return institution;
    }
}
