namespace AethericGm.Core.Places;

public static class PlaceHierarchy
{
    public static bool WouldCreateCycle(IReadOnlyList<Place> places, Guid placeId, Guid? proposedParentId)
    {
        if (proposedParentId is null) return false;
        if (proposedParentId == placeId) return true;
        var byId = places.ToDictionary(place => place.Id);
        var current = proposedParentId;
        var guard = 0;
        while (current is { } id && guard++ <= places.Count)
        {
            if (id == placeId) return true;
            current = byId.TryGetValue(id, out var place) ? place.ParentId : null;
        }
        return false;
    }

    public static string ContainmentPath(IReadOnlyList<Place> places, Place place)
    {
        ArgumentNullException.ThrowIfNull(place);
        var byId = places.ToDictionary(candidate => candidate.Id);
        var segments = new List<string> { place.Name };
        var current = place.ParentId;
        var guard = 0;
        while (current is { } id && guard++ < places.Count)
        {
            if (!byId.TryGetValue(id, out var parent)) break;
            segments.Insert(0, parent.Name);
            current = parent.ParentId;
        }
        return string.Join(" / ", segments);
    }

    public static IEnumerable<Place> Children(IReadOnlyList<Place> places, Guid? parentId) =>
        places.Where(place => place.ParentId == parentId).OrderBy(place => place.Name, StringComparer.OrdinalIgnoreCase);
}
