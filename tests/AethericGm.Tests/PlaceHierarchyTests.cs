using AethericGm.Core.Places;

namespace AethericGm.Tests;

public class PlaceHierarchyTests
{
    [Fact]
    public void WouldCreateCycle_detects_a_multi_level_cycle()
    {
        var campaignId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var a = Place.Create(campaignId, "A", null, now);
        var b = Place.Create(campaignId, "B", a.Id, now);
        var c = Place.Create(campaignId, "C", b.Id, now);
        var places = new[] { a, b, c };

        Assert.True(PlaceHierarchy.WouldCreateCycle(places, a.Id, c.Id));
        Assert.True(PlaceHierarchy.WouldCreateCycle(places, a.Id, a.Id));
        Assert.False(PlaceHierarchy.WouldCreateCycle(places, c.Id, a.Id));
        Assert.False(PlaceHierarchy.WouldCreateCycle(places, a.Id, null));
    }

    [Fact]
    public void ContainmentPath_joins_ancestor_names_from_root_to_leaf()
    {
        var campaignId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var region = Place.Create(campaignId, "Coldharbor", null, now);
        var city = Place.Create(campaignId, "Ashport", region.Id, now);
        var district = Place.Create(campaignId, "Docks", city.Id, now);
        var places = new[] { region, city, district };

        Assert.Equal("Coldharbor", PlaceHierarchy.ContainmentPath(places, region));
        Assert.Equal("Coldharbor / Ashport", PlaceHierarchy.ContainmentPath(places, city));
        Assert.Equal("Coldharbor / Ashport / Docks", PlaceHierarchy.ContainmentPath(places, district));
    }

    [Fact]
    public void Children_returns_only_direct_children_ordered_by_name()
    {
        var campaignId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var region = Place.Create(campaignId, "Coldharbor", null, now);
        var zed = Place.Create(campaignId, "Zed's Landing", region.Id, now);
        var ashport = Place.Create(campaignId, "Ashport", region.Id, now);
        var district = Place.Create(campaignId, "Docks", ashport.Id, now);
        var places = new[] { region, zed, ashport, district };

        Assert.Equal(["Ashport", "Zed's Landing"], PlaceHierarchy.Children(places, region.Id).Select(place => place.Name));
        Assert.Equal(["Docks"], PlaceHierarchy.Children(places, ashport.Id).Select(place => place.Name));
        Assert.Equal(["Coldharbor"], PlaceHierarchy.Children(places, null).Select(place => place.Name));
    }
}
