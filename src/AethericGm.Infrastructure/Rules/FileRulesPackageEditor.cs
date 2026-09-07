using System.Text.Json;
using System.Text.Json.Serialization;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Catalog;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Infrastructure.Rules;

public sealed class FileRulesPackageEditor(string packagePath)
{
    private static readonly string[] PackageFiles = ["manifest.json", "record-types.json", "records.json", "character-sheet.json", "catalog.json"];
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) }
    };

    public Task SaveCatalogAsync(RulesetReference ruleset, RulesCatalogDefinition catalog, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var document = new CatalogDocument(catalog.Sections.Select(section => new CatalogSectionDocument(
            section.Key, section.Label, section.Items.Select(ToDocument).ToArray())).ToArray());
        return SaveAsync(ruleset, "catalog.json", document, ct);
    }

    public Task SaveRecordsAsync(RulesetReference ruleset, IEnumerable<RulesRecord> records, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        var document = new RecordsDocument(records.Select(record => new RulesRecordDocument(record.Key, record.RecordType, record.Values)).ToArray());
        return SaveAsync(ruleset, "records.json", document, ct);
    }

    private async Task SaveAsync<T>(RulesetReference ruleset, string fileName, T document, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ruleset);
        if (!Path.IsPathFullyQualified(packagePath)) throw new ArgumentException("Installed package path must be fully qualified.", nameof(packagePath));
        if (!Directory.Exists(packagePath)) throw new DirectoryNotFoundException($"Installed package directory '{packagePath}' was not found.");
        var packageParent = Path.GetDirectoryName(Path.GetFullPath(packagePath))
            ?? throw new InvalidDataException("Installed package directory has no parent directory.");
        var validationRoot = Path.Combine(packageParent, $".{Path.GetFileName(packagePath)}-edit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(validationRoot);
        try
        {
            foreach (var file in PackageFiles)
            {
                var source = Path.Combine(packagePath, file);
                if (File.Exists(source)) File.Copy(source, Path.Combine(validationRoot, file));
            }
            if (!File.Exists(Path.Combine(validationRoot, "manifest.json")))
                throw new InvalidDataException("The installed package has no manifest.json to validate.");
            await WriteDocumentAsync(Path.Combine(validationRoot, fileName), document, ct);
            var candidates = new FileRulesCatalog(validationRoot).List();
            var validated = candidates.SingleOrDefault(candidate =>
                candidate.Reference.Id == ruleset.Id && candidate.Reference.Version == ruleset.Version)
                ?? throw new InvalidDataException($"Edited package validation found '{string.Join(", ", candidates.Select(candidate => candidate.Reference.ToString()))}' instead of '{ruleset}'.");
            _ = validated;

            var target = Path.Combine(packagePath, fileName);
            var temporary = Path.Combine(packagePath, $".{fileName}.{Guid.NewGuid():N}.tmp");
            try
            {
                await WriteDocumentAsync(temporary, document, ct);
                File.Move(temporary, target, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        finally { if (Directory.Exists(validationRoot)) Directory.Delete(validationRoot, true); }
    }

    private static async Task WriteDocumentAsync<T>(string path, T document, CancellationToken ct)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, Options, ct);
        await stream.FlushAsync(ct);
    }

    private static CatalogItemDocument ToDocument(RulesCatalogItem item) =>
        new(item.Key, item.Label, item.Kind, item.RecordType, item.Items.Select(ToDocument).ToArray());

    private sealed record RecordsDocument(IReadOnlyList<RulesRecordDocument> Records);
    private sealed record RulesRecordDocument(string Key, string RecordType, IReadOnlyDictionary<string, JsonElement> Values);
    private sealed record CatalogDocument(IReadOnlyList<CatalogSectionDocument> Sections);
    private sealed record CatalogSectionDocument(string Key, string Label, IReadOnlyList<CatalogItemDocument> Items);
    private sealed record CatalogItemDocument(string Key, string Label, RulesCatalogItemKind Kind, string? RecordType, IReadOnlyList<CatalogItemDocument> Items);
}
