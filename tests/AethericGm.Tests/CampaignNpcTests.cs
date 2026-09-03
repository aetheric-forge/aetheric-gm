using AethericGm.Core.Npcs;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Tests;

public class CampaignNpcTests
{
    [Fact] public void Name_is_required() => Assert.Throws<ArgumentException>(() => CampaignNpc.Create(Guid.NewGuid(), "  ", null, DateTimeOffset.UtcNow));

    [Fact]
    public void Update_normalizes_fields_and_deduplicates_tags()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var npc = CampaignNpc.Create(Guid.NewGuid(), "  Grask  ", null, now);
        npc.Update("Grask the Wary", " Keeps to the shadows. ", ["Goblin", "goblin", " Sentinel "], " Wary ", " Old Watchtower ", " Alive ", now.AddHours(1));

        Assert.Equal("Grask the Wary", npc.Name);
        Assert.Equal("Keeps to the shadows.", npc.Notes);
        Assert.Equal(["Goblin", "Sentinel"], npc.Tags);
        Assert.Equal("Wary", npc.Disposition);
        Assert.Equal("Old Watchtower", npc.Location);
        Assert.Equal("Alive", npc.Status);
        Assert.Equal(now.AddHours(1), npc.UpdatedAt);
    }

    [Fact]
    public void Source_is_immutable_after_creation()
    {
        var source = new NpcSource(new RulesetReference("test", "1.0.0"), new RulesRecordReference("npc", "goblin"));
        var npc = CampaignNpc.Create(Guid.NewGuid(), "Grask", source, DateTimeOffset.UtcNow);
        npc.Update("Grask the Wary", null, [], null, null, null, DateTimeOffset.UtcNow);
        Assert.Equal(source, npc.Source);
    }

    [Fact]
    public void UpdateResources_replaces_the_list_and_rejects_negative_values()
    {
        var npc = CampaignNpc.Create(Guid.NewGuid(), "Grask", null, DateTimeOffset.UtcNow);
        npc.UpdateResources([new NpcResource("HP", 12, 12)], DateTimeOffset.UtcNow);
        Assert.Equal(12, Assert.Single(npc.Resources).Current);
        npc.UpdateResources([new NpcResource("HP", 7, 12)], DateTimeOffset.UtcNow);
        Assert.Equal(7, Assert.Single(npc.Resources).Current);
        Assert.Throws<ArgumentOutOfRangeException>(() => new NpcResource("HP", -1, 12));
    }
}
