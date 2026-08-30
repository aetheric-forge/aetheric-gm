using System.Text.Json;
using System.Text.Json.Serialization;
using AethericGm.Core.Rules;
namespace AethericGm.Infrastructure.Rules;
public sealed class FileRulesCatalog : IRulesCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private readonly IReadOnlyDictionary<RulesetReference, RulesetDescriptor> rulesets;
    public FileRulesCatalog(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath)) throw new ArgumentException("Rules catalog path is required.", nameof(rootPath));
        if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException($"Rules catalog directory '{rootPath}' was not found.");
        var loaded = new Dictionary<RulesetReference, RulesetDescriptor>();
        foreach (var path in Directory.EnumerateFiles(rootPath, "manifest.json", SearchOption.AllDirectories).Order())
        {
            var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Ruleset manifest '{path}' is empty.");
            var descriptor = new RulesetDescriptor(new RulesetReference(manifest.Id, manifest.Version), manifest.Name, manifest.Description);
            if (!loaded.TryAdd(descriptor.Reference, descriptor)) throw new InvalidDataException($"Duplicate ruleset '{descriptor.Reference}' in '{path}'.");
        }
        rulesets = loaded;
    }
    public IReadOnlyList<RulesetDescriptor> List() => rulesets.Values.OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase).ThenBy(value => value.Reference.Version, StringComparer.Ordinal).ToArray();
    public RulesetDescriptor? Resolve(RulesetReference reference) { ArgumentNullException.ThrowIfNull(reference); return rulesets.GetValueOrDefault(reference); }
    private sealed record Manifest(string Id, string Version, string Name, string? Description);
}
