using AethericGm.Core.Campaigns;
using AethericGm.Core.Rules;
namespace AethericGm.Tests;
public class CampaignTests
{
    [Fact] public void Name_is_required() => Assert.Throws<ArgumentException>(() => Campaign.Create("  ", DateTimeOffset.UtcNow));
    [Fact] public void Metadata_is_normalized_and_archive_can_be_reversed()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z"); var campaign = Campaign.Create("  Ember Coast ", now);
        campaign.Update(campaign.Name, "  Cairn ", " ", "  A storm approaches. ", now.AddHours(1)); campaign.Archive(now.AddHours(2));
        Assert.Equal("Ember Coast", campaign.Name); Assert.Equal("Cairn", campaign.System); Assert.Null(campaign.Setting); Assert.NotNull(campaign.ArchivedAt);
        campaign.Restore(now.AddHours(3)); Assert.Null(campaign.ArchivedAt);
    }
    [Fact] public void Ruleset_selection_is_explicit_and_versioned()
    {
        var campaign = Campaign.Create("Ashes", DateTimeOffset.UtcNow); var reference = new RulesetReference("example-fantasy", "1.0.0");
        campaign.SelectRuleset(reference, DateTimeOffset.UtcNow); Assert.Equal(reference, campaign.Ruleset);
        campaign.SelectRuleset(null, DateTimeOffset.UtcNow); Assert.Null(campaign.Ruleset);
    }
}
