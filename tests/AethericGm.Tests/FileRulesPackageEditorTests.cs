using System.Text.Json;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.Catalog;
using AethericGm.Core.Rules.Records;
using AethericGm.Infrastructure.Rules;

namespace AethericGm.Tests;

public sealed class FileRulesPackageEditorTests : IDisposable
{
    private readonly string package = Path.Combine(Path.GetTempPath(), $"aetheric-package-editor-{Guid.NewGuid():N}");
    private static readonly RulesetReference Reference = new("test", "1.0.0");

    public FileRulesPackageEditorTests()
    {
        Directory.CreateDirectory(package);
        File.WriteAllText(Path.Combine(package, "manifest.json"), """{"id":"test","version":"1.0.0","name":"Test"}""");
        File.WriteAllText(Path.Combine(package, "record-types.json"), """
        {"recordTypes":[
          {"key":"named","label":"Named","displayField":"name","fields":[{"key":"name","label":"Name","valueKind":"text","cardinality":"one"}]},
          {"key":"ancestry","label":"Ancestry","extends":"named","displayField":"name","fields":[]}
        ]}
        """);
    }

    public void Dispose() => Directory.Delete(package, true);

    [Fact]
    public async Task Atomically_saves_valid_catalogs_and_records()
    {
        var editor = new FileRulesPackageEditor(package);
        var catalog = new RulesCatalogDefinition([new RulesCatalogSection("character-creation", "Character Creation",
            [new RulesCatalogItem("ancestries", "Ancestries", RulesCatalogItemKind.RecordCatalog, "ancestry")])]);
        var values = new Dictionary<string, JsonElement> { ["name"] = JsonSerializer.SerializeToElement("Elf") };

        await editor.SaveCatalogAsync(Reference, catalog);
        await editor.SaveRecordsAsync(Reference, [new RulesRecord("elf", "ancestry", values)]);

        var descriptor = new FileRulesCatalog(package).Resolve(Reference)!;
        Assert.Equal("ancestries", Assert.Single(Assert.Single(descriptor.Catalog.Sections).Items).Key);
        Assert.Equal("elf", Assert.Single(descriptor.Records).Key);
    }

    [Fact]
    public async Task Failed_validation_preserves_the_previous_records_document()
    {
        var original = """{"records":[]}""";
        File.WriteAllText(Path.Combine(package, "records.json"), original);
        var values = new Dictionary<string, JsonElement> { ["unknown"] = JsonSerializer.SerializeToElement("value") };

        await Assert.ThrowsAsync<InvalidDataException>(() => new FileRulesPackageEditor(package)
            .SaveRecordsAsync(Reference, [new RulesRecord("elf", "ancestry", values)]));

        Assert.Equal(original, File.ReadAllText(Path.Combine(package, "records.json")));
    }
}
