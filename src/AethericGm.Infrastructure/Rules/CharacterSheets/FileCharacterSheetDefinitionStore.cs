using System.Text.Json;
using System.Text.Json.Serialization;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;

namespace AethericGm.Infrastructure.Rules.CharacterSheets;

public sealed class FileCharacterSheetDefinitionStore(string rootPath) : ICharacterSheetDefinitionStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<CharacterSheetDefinition?> GetAsync(RulesetReference ruleset, CancellationToken ct = default)
    {
        var path = PathFor(ruleset); if (!File.Exists(path)) return null;
        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<Document>(stream, Options, ct) ?? throw new InvalidDataException($"Character sheet '{path}' is empty.");
        if (document.RulesetId != ruleset.Id || document.RulesetVersion != ruleset.Version) throw new InvalidDataException($"Character sheet '{path}' does not match ruleset '{ruleset}'.");
        return new CharacterSheetDefinition(ruleset, document.Sections.Select(section => new CharacterSheetSection(section.Key, section.Label,
            section.Fields.Select(field => new CharacterSheetField(field.Key, field.Label, field.Type, field.Required, field.Choices)))));
    }

    public async Task SaveAsync(CharacterSheetDefinition definition, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(definition); var path = PathFor(definition.Ruleset); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var document = new Document(definition.Ruleset.Id, definition.Ruleset.Version,
            definition.Sections.Select(section => new SectionDocument(section.Key, section.Label,
                section.Fields.Select(field => new FieldDocument(field.Key, field.Label, field.Type, field.Required, field.Choices)).ToArray())).ToArray());
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try { await using (var stream = File.Create(temporary)) await JsonSerializer.SerializeAsync(stream, document, Options, ct); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private string PathFor(RulesetReference ruleset) => Path.Combine(rootPath, ruleset.Id, ruleset.Version, "character-sheet.json");
    private sealed record Document(string RulesetId, string RulesetVersion, IReadOnlyList<SectionDocument> Sections);
    private sealed record SectionDocument(string Key, string Label, IReadOnlyList<FieldDocument> Fields);
    private sealed record FieldDocument(string Key, string Label, CharacterFieldType Type, bool Required, IReadOnlyList<string> Choices);
}
