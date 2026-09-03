using AethericGm.Core.Entities;
using AethericGm.Core.People;

namespace AethericGm.Tests;

public class CampaignEntityTests
{
    [Fact] public void Name_is_required() => Assert.Throws<ArgumentException>(() => CampaignEntity.Create(Guid.NewGuid(), EntityKind.Person, "  ", DateTimeOffset.UtcNow));

    [Fact] public void Only_person_or_faction_kinds_are_accepted() =>
        Assert.Throws<ArgumentException>(() => CampaignEntity.Create(Guid.NewGuid(), EntityKind.Npc, "Grask", DateTimeOffset.UtcNow));

    [Fact]
    public void Update_normalizes_fields_deduplicates_tags_and_tracks_the_secret_flag()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var placeId = Guid.NewGuid();
        var entity = CampaignEntity.Create(Guid.NewGuid(), EntityKind.Faction, "  The Ashen Company  ", now);
        entity.Update("The Ashen Company", " Mercenaries for hire. ", true, ["Mercenary", "mercenary", " Guild "], " Guild ", " Active ", placeId, now.AddHours(1));

        Assert.Equal("The Ashen Company", entity.Name);
        Assert.Equal("Mercenaries for hire.", entity.Notes);
        Assert.True(entity.NotesAreSecret);
        Assert.Equal(["Mercenary", "Guild"], entity.Tags);
        Assert.Equal("Guild", entity.Role);
        Assert.Equal("Active", entity.Status);
        Assert.Equal(placeId, entity.PlaceId);
        Assert.Equal(now.AddHours(1), entity.UpdatedAt);
    }
}
