namespace AethericGm.Core.Places;

public sealed class Place
{
    private Place(Guid id, Guid campaignId, string name, Guid? parentId, string? notes, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Place ID is required.", nameof(id));
        if (campaignId == Guid.Empty) throw new ArgumentException("Campaign ID is required.", nameof(campaignId));
        if (parentId == id) throw new ArgumentException("A place cannot contain itself.", nameof(parentId));
        Id = id;
        CampaignId = campaignId;
        Name = RequireName(name);
        ParentId = parentId;
        Notes = Normalize(notes);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid CampaignId { get; }
    public string Name { get; private set; }
    public Guid? ParentId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Place Create(Guid campaignId, string name, Guid? parentId, DateTimeOffset now) =>
        new(Guid.NewGuid(), campaignId, name, parentId, null, now, now);

    public static Place Rehydrate(Guid id, Guid campaignId, string name, Guid? parentId, string? notes, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(id, campaignId, name, parentId, notes, createdAt, updatedAt);

    public void Update(string name, Guid? parentId, string? notes, DateTimeOffset now)
    {
        if (parentId == Id) throw new ArgumentException("A place cannot contain itself.", nameof(parentId));
        Name = RequireName(name);
        ParentId = parentId;
        Notes = Normalize(notes);
        UpdatedAt = now;
    }

    private static string RequireName(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Place name is required.", nameof(value)) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
