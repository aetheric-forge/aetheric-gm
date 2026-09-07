using AethericGm.Core.Entities;

namespace AethericGm.Core.Relationships;

public sealed class Relationship
{
    private Relationship(Guid id, Guid campaignId, EntityReference from, EntityReference to, string label,
        bool isSymmetric, bool isSecret, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Relationship ID is required.", nameof(id));
        if (campaignId == Guid.Empty) throw new ArgumentException("Campaign ID is required.", nameof(campaignId));
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        if (from == to) throw new ArgumentException("A relationship cannot connect an entity to itself.", nameof(to));
        Id = id;
        CampaignId = campaignId;
        From = from;
        To = to;
        Label = RequireLabel(label);
        IsSymmetric = isSymmetric;
        IsSecret = isSecret;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid CampaignId { get; }
    public EntityReference From { get; }
    public EntityReference To { get; }
    public string Label { get; private set; }
    public bool IsSymmetric { get; private set; }
    public bool IsSecret { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Relationship Create(Guid campaignId, EntityReference from, EntityReference to, string label, bool isSymmetric, bool isSecret, DateTimeOffset now) =>
        new(Guid.NewGuid(), campaignId, from, to, label, isSymmetric, isSecret, now, now);

    public static Relationship Rehydrate(Guid id, Guid campaignId, EntityReference from, EntityReference to, string label,
        bool isSymmetric, bool isSecret, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(id, campaignId, from, to, label, isSymmetric, isSecret, createdAt, updatedAt);

    public void Update(string label, bool isSymmetric, bool isSecret, DateTimeOffset now)
    {
        Label = RequireLabel(label);
        IsSymmetric = isSymmetric;
        IsSecret = isSecret;
        UpdatedAt = now;
    }

    public bool Involves(EntityReference entity) => From == entity || To == entity;

    private static string RequireLabel(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("A relationship label is required.", nameof(value)) : value.Trim();
}
