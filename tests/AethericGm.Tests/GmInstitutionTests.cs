using AethericGm.Core.Npcs;
using AethericGm.Core.People;
using AethericGm.Core.Places;
using AethericGm.Core.Entities;
using AethericGm.Core.Relationships;
using AethericForge.Runtime.Institutions.Abstractions.Builders;
using AethericForge.Runtime.Institutions.Campus;
using AethericForge.Runtime.Institutions.Registry;
using AethericForge.Runtime.Abstractions.Interfaces.Identity.Services;
using AethericForge.Runtime.Models.Authorities;
using AethericForge.Runtime.Services.Identity;
using AethericForge.Runtime.Services.Identity.Lifecycle;
using AethericForge.Runtime.Services.Registry;
using AethericGm.Core.Campaigns;
using AethericGm.Core.Profiles;
using AethericGm.Infrastructure.Composition;
using AethericGm.Institutions.Gm;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace AethericGm.Tests;

public sealed class GmInstitutionTests
{
    [Fact]
    public async Task Existing_campus_hosts_Gm_without_web_host_and_preserves_data_when_rehosted()
    {
        using var storage = new GmTestStorage();
        Guid campaignId;
        await using (var services = storage.CreateServices())
        {
            await services.InitializeLocalGmStorageAsync();
            var campus = CreateCampus(services, "Standalone GM");
            var gm = campus.RegisterAethericGm(services);
            var campaign = Campaign.Create("Portable campaign", DateTimeOffset.UtcNow);
            campaignId = campaign.Id;
            await gm.Campaigns.SaveAsync(campaign);
            await gm.Campaigns.SetSelectedIdAsync(campaignId);
            Assert.Same(services.GetRequiredService<ICampaignRepository>(), gm.Campaigns);
        }

        await using (var services = storage.CreateServices())
        {
            await services.InitializeLocalGmStorageAsync();
            var campus = CreateCampus(services, "Greater Forge");
            var identity = new IdentityService([], new IdentityLifecycleService([]));
            var registry = new Registry(new RegistryContext(campus.Context.Template, services, campus),
                new RegistryService(identity, new Team<IRegistryClerk>([])), new Registrar(identity));
            campus.Register<IRegistry>(registry);
            var gm = campus.RegisterAethericGm(services);

            Assert.Same(campus, gm.Context.Parent);
            Assert.Same(gm, campus.Resolve<IAethericGm>());
            Assert.Same(registry, gm.Resolve<IRegistry>());
            Assert.Equal(campaignId, await gm.Campaigns.GetSelectedIdAsync());
            Assert.Equal("Portable campaign", (await gm.Campaigns.GetAsync(campaignId))?.Name);
            Assert.Throws<InvalidOperationException>(() => campus.RegisterAethericGm(services));
            Assert.Same(gm, campus.Resolve<IAethericGm>());
        }
    }

    [Fact]
    public async Task Campaign_world_capabilities_preserve_data_across_hosts()
    {
        using var storage = new GmTestStorage();
        var campaign = Campaign.Create("World", DateTimeOffset.UtcNow);
        var npc = CampaignNpc.Create(campaign.Id, "Guide", null, DateTimeOffset.UtcNow);
        var faction = CampaignEntity.Create(campaign.Id, EntityKind.Faction, "Guild", DateTimeOffset.UtcNow);
        var place = Place.Create(campaign.Id, "Harbor", null, DateTimeOffset.UtcNow);
        var source = new EntityReference(EntityKind.Npc, npc.Id);
        var relationship = Relationship.Create(campaign.Id, source,
            new EntityReference(EntityKind.Faction, faction.Id), "member", false, false, DateTimeOffset.UtcNow);
        await using (var services = storage.CreateServices())
        {
            await services.InitializeLocalGmStorageAsync();
            var gm = CreateCampus(services, "Original host").RegisterAethericGm(services);
            await gm.Campaigns.SaveAsync(campaign);
            await gm.Npcs.SaveAsync(npc);
            await gm.People.SaveAsync(faction);
            await gm.Places.SaveAsync(place);
            await gm.Relationships.SaveAsync(relationship);
        }
        await using (var services = storage.CreateServices())
        {
            await services.InitializeLocalGmStorageAsync();
            var gm = CreateCampus(services, "New host").RegisterAethericGm(services);
            Assert.Equal(npc.Id, Assert.Single(await gm.Npcs.ListAsync(campaign.Id)).Id);
            Assert.Equal(faction.Id, Assert.Single(await gm.People.ListAsync(campaign.Id)).Id);
            Assert.Equal(place.Id, Assert.Single(await gm.Places.ListAsync(campaign.Id)).Id);
            Assert.Equal(relationship.Id, Assert.Single(await gm.Relationships.ListForEntityAsync(campaign.Id, source)).Id);
        }
    }

    [Fact]
    public void Institution_requires_a_parent_and_has_no_web_or_storage_dependency()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var template = InstitutionTemplateBuilder.Create().UseModule<CampusModule>().Build();
        Assert.Throws<ArgumentNullException>(() => new AethericGmContext(template, services, null!));
        var references = typeof(AethericGmInstitution).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("AethericGm.Web", references);
        Assert.DoesNotContain("AethericGm.Infrastructure", references);
        Assert.DoesNotContain("Microsoft.AspNetCore", references);
    }

    [Fact]
    public void Storage_rejects_working_directory_dependent_paths()
    {
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddLocalGmStorage(
            new LocalGmStorageOptions("App_Data", "rulesets")));
    }

    private static Campus CreateCampus(IServiceProvider services, string name)
    {
        var template = InstitutionTemplateBuilder.Create()
            .WithDescriptor(name, new Version(1, 0), "Test host").Build();
        return new Campus(new CampusContext(template, services));
    }
}

internal sealed class GmTestStorage : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"gm-host-test-{Guid.NewGuid():N}");

    public ServiceProvider CreateServices()
    {
        Directory.CreateDirectory(Path.Combine(root, "rulesets"));
        var services = new ServiceCollection();
        services.AddSingleton<ISshPrivateKeyProtector>(new UnusedPrivateKeyProtector());
        services.AddLocalGmStorage(new LocalGmStorageOptions(root, Path.Combine(root, "rulesets")));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }

    private sealed class UnusedPrivateKeyProtector : ISshPrivateKeyProtector
    {
        public string Protect(string privateKey) => throw new NotSupportedException();
        public string Unprotect(string protectedPrivateKey) => throw new NotSupportedException();
    }
}
