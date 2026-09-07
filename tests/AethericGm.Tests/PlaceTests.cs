using AethericGm.Core.Places;

namespace AethericGm.Tests;

public class PlaceTests
{
    [Fact] public void Name_is_required() => Assert.Throws<ArgumentException>(() => Place.Create(Guid.NewGuid(), "  ", null, DateTimeOffset.UtcNow));

    [Fact]
    public void Update_rejects_a_place_containing_itself()
    {
        var place = Place.Create(Guid.NewGuid(), "Coldharbor", null, DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentException>(() => place.Update("Coldharbor", place.Id, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Update_normalizes_name_parent_and_notes()
    {
        var now = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var parentId = Guid.NewGuid();
        var place = Place.Create(Guid.NewGuid(), "  Docks  ", null, now);
        place.Update("The Docks", parentId, " Smells of fish. ", now.AddHours(1));

        Assert.Equal("The Docks", place.Name);
        Assert.Equal(parentId, place.ParentId);
        Assert.Equal("Smells of fish.", place.Notes);
        Assert.Equal(now.AddHours(1), place.UpdatedAt);
    }
}
