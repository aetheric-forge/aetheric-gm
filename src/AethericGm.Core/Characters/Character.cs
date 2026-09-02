using System.Text.Json;
using AethericGm.Core.Rules;

namespace AethericGm.Core.Characters;

public sealed class Character
{
    private readonly Dictionary<string, JsonElement> values;

    private Character(Guid id, Guid campaignId, RulesetReference ruleset, IReadOnlyDictionary<string, JsonElement> values,
        DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Character ID is required.", nameof(id));
        if (campaignId == Guid.Empty) throw new ArgumentException("Campaign ID is required.", nameof(campaignId));
        Id = id;
        CampaignId = campaignId;
        Ruleset = ruleset ?? throw new ArgumentNullException(nameof(ruleset));
        this.values = Clone(values);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; }
    public Guid CampaignId { get; }
    public RulesetReference Ruleset { get; }
    public IReadOnlyDictionary<string, JsonElement> Values => values;
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string Name => values.TryGetValue("name", out var value) && value.ValueKind == JsonValueKind.String
        ? value.GetString() ?? "Unnamed character"
        : "Unnamed character";

    public static Character Create(Guid campaignId, RulesetReference ruleset, IReadOnlyDictionary<string, JsonElement> values, DateTimeOffset now) =>
        new(Guid.NewGuid(), campaignId, ruleset, values, now, now);

    public static Character Rehydrate(Guid id, Guid campaignId, RulesetReference ruleset, IReadOnlyDictionary<string, JsonElement> values,
        DateTimeOffset createdAt, DateTimeOffset updatedAt) => new(id, campaignId, ruleset, values, createdAt, updatedAt);

    public void UpdateValues(IReadOnlyDictionary<string, JsonElement> updatedValues, DateTimeOffset now)
    {
        var replacement = Clone(updatedValues);
        values.Clear();
        foreach (var pair in replacement) values.Add(pair.Key, pair.Value);
        UpdatedAt = now;
    }

    private static Dictionary<string, JsonElement> Clone(IReadOnlyDictionary<string, JsonElement> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.ToDictionary(pair => RequireKey(pair.Key), pair => pair.Value.Clone(), StringComparer.Ordinal);
    }

    private static string RequireKey(string value) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Character value key is required.", nameof(value)) : value.Trim();
}
