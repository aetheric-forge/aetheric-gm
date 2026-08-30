namespace AethericGm.Core.Campaigns;

public sealed class Campaign
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? System { get; private set; }
    public string? Setting { get; private set; }
    public string? Summary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    private Campaign(Guid id, string name, string? system, string? setting, string? summary,
        DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? archivedAt)
    {
        Id = id;
        Name = name;
        System = system;
        Setting = setting;
        Summary = summary;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ArchivedAt = archivedAt;
    }

    public static Campaign Create(string name, DateTimeOffset now) =>
        new(Guid.NewGuid(), RequireName(name), null, null, null, now, now, null);

    public static Campaign Rehydrate(Guid id, string name, string? system, string? setting,
        string? summary, DateTimeOffset createdAt, DateTimeOffset updatedAt, DateTimeOffset? archivedAt) =>
        new(id, RequireName(name), system, setting, summary, createdAt, updatedAt, archivedAt);

    public void Update(string name, string? system, string? setting, string? summary, DateTimeOffset now)
    {
        Name = RequireName(name);
        System = Normalize(system);
        Setting = Normalize(setting);
        Summary = Normalize(summary);
        UpdatedAt = now;
    }

    public void Archive(DateTimeOffset now) { ArchivedAt ??= now; UpdatedAt = now; }
    public void Restore(DateTimeOffset now) { ArchivedAt = null; UpdatedAt = now; }

    private static string RequireName(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Campaign name is required.", nameof(value)) : value.Trim();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
