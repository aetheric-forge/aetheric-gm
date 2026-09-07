using AethericGm.Core.Entities;

namespace AethericGm.Core.People;

public sealed class CampaignEntity
{
    private readonly List<string> tags;

    private CampaignEntity(Guid id, Guid campaignId, EntityKind kind, string name, string? notes, bool notesAreSecret,
        IReadOnlyList<string> tags, string? role, string? status, Guid? placeId, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Entity ID is required.", nameof(id));
        if (campaignId == Guid.Empty) throw new ArgumentException("Campaign ID is required.", nameof(campaignId));
        if (kind is not (EntityKind.Person or EntityKind.Faction)) throw new ArgumentException("A campaign entity must be a person or a faction.", nameof(kind));
        Id = id;
        CampaignId = campaignId;
        Kind = kind;
        Name = RequireName(name);
        Notes = Normalize(notes);
        NotesAreSecret = notesAreSecret;
        this.tags = NormalizeTags(tags);
        Role = Normalize(role);
        Status = Normalize(status);
        PlaceId = placeId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid CampaignId { get; }
    public EntityKind Kind { get; }
    public string Name { get; private set; }
    public string? Notes { get; private set; }
    public bool NotesAreSecret { get; private set; }
    public IReadOnlyList<string> Tags => tags;
    public string? Role { get; private set; }
    public string? Status { get; private set; }
    public Guid? PlaceId { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CampaignEntity Create(Guid campaignId, EntityKind kind, string name, DateTimeOffset now) =>
        new(Guid.NewGuid(), campaignId, kind, name, null, false, [], null, null, null, now, now);

    public static CampaignEntity Rehydrate(Guid id, Guid campaignId, EntityKind kind, string name, string? notes, bool notesAreSecret,
        IReadOnlyList<string> tags, string? role, string? status, Guid? placeId, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(id, campaignId, kind, name, notes, notesAreSecret, tags, role, status, placeId, createdAt, updatedAt);

    public void Update(string name, string? notes, bool notesAreSecret, IReadOnlyList<string> tags, string? role, string? status, Guid? placeId, DateTimeOffset now)
    {
        Name = RequireName(name);
        Notes = Normalize(notes);
        NotesAreSecret = notesAreSecret;
        this.tags.Clear();
        this.tags.AddRange(NormalizeTags(tags));
        Role = Normalize(role);
        Status = Normalize(status);
        PlaceId = placeId;
        UpdatedAt = now;
    }

    private static string RequireName(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Name is required.", nameof(value)) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static List<string> NormalizeTags(IReadOnlyList<string>? values) =>
        (values ?? []).Select(value => value.Trim()).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
