using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Core.Npcs;

public sealed record NpcSource
{
    public NpcSource(RulesetReference ruleset, RulesRecordReference record)
    {
        Ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
        Record = record ?? throw new ArgumentNullException(nameof(record));
    }
    public RulesetReference Ruleset { get; }
    public RulesRecordReference Record { get; }
}

public sealed record NpcResource
{
    public NpcResource(string label, int current, int max)
    {
        Label = string.IsNullOrWhiteSpace(label) ? throw new ArgumentException("Resource label is required.", nameof(label)) : label.Trim();
        if (current < 0) throw new ArgumentOutOfRangeException(nameof(current), "Current value cannot be negative.");
        if (max < 0) throw new ArgumentOutOfRangeException(nameof(max), "Maximum value cannot be negative.");
        Current = current;
        Max = max;
    }
    public string Label { get; }
    public int Current { get; }
    public int Max { get; }
}

public sealed class CampaignNpc
{
    private readonly List<string> tags;
    private readonly List<NpcResource> resources;

    private CampaignNpc(Guid id, Guid campaignId, NpcSource? source, string name, string? notes,
        IReadOnlyList<string> tags, string? disposition, string? location, Guid? placeId, string? status,
        IReadOnlyList<NpcResource> resources, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("NPC ID is required.", nameof(id));
        if (campaignId == Guid.Empty) throw new ArgumentException("Campaign ID is required.", nameof(campaignId));
        Id = id;
        CampaignId = campaignId;
        Source = source;
        Name = RequireName(name);
        Notes = Normalize(notes);
        this.tags = NormalizeTags(tags);
        Disposition = Normalize(disposition);
        Location = Normalize(location);
        PlaceId = placeId;
        Status = Normalize(status);
        this.resources = [.. resources ?? []];
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid CampaignId { get; }
    public NpcSource? Source { get; }
    public string Name { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyList<string> Tags => tags;
    public string? Disposition { get; private set; }
    public string? Location { get; private set; }
    public Guid? PlaceId { get; private set; }
    public string? Status { get; private set; }
    public IReadOnlyList<NpcResource> Resources => resources;
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static CampaignNpc Create(Guid campaignId, string name, NpcSource? source, DateTimeOffset now) =>
        new(Guid.NewGuid(), campaignId, source, name, null, [], null, null, null, null, [], now, now);

    public static CampaignNpc Rehydrate(Guid id, Guid campaignId, NpcSource? source, string name, string? notes,
        IReadOnlyList<string> tags, string? disposition, string? location, Guid? placeId, string? status,
        IReadOnlyList<NpcResource> resources, DateTimeOffset createdAt, DateTimeOffset updatedAt) =>
        new(id, campaignId, source, name, notes, tags, disposition, location, placeId, status, resources, createdAt, updatedAt);

    public void Update(string name, string? notes, IReadOnlyList<string> tags, string? disposition, string? location, Guid? placeId, string? status, DateTimeOffset now)
    {
        Name = RequireName(name);
        Notes = Normalize(notes);
        this.tags.Clear();
        this.tags.AddRange(NormalizeTags(tags));
        Disposition = Normalize(disposition);
        Location = Normalize(location);
        PlaceId = placeId;
        Status = Normalize(status);
        UpdatedAt = now;
    }

    public void UpdateResources(IReadOnlyList<NpcResource> updated, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(updated);
        resources.Clear();
        resources.AddRange(updated);
        UpdatedAt = now;
    }

    private static string RequireName(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("NPC name is required.", nameof(value)) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static List<string> NormalizeTags(IReadOnlyList<string>? values) =>
        (values ?? []).Select(value => value.Trim()).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
