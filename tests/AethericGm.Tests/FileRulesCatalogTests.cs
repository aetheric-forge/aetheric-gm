using AethericGm.Core.Rules;
using AethericGm.Infrastructure.Rules;

namespace AethericGm.Tests;

public sealed class FileRulesCatalogTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"aetheric-rules-{Guid.NewGuid():N}");
    public FileRulesCatalogTests() => Directory.CreateDirectory(root);
    public void Dispose() => Directory.Delete(root, true);

    [Fact]
    public void Discovers_and_resolves_versioned_manifests()
    {
        WriteManifest("shadowdark", "1.0.0", "Shadowdark RPG");
        var catalog = new FileRulesCatalog(root);
        var descriptor = Assert.Single(catalog.List());
        Assert.Equal("Shadowdark RPG", descriptor.Name);
        Assert.Same(descriptor, catalog.Resolve(new RulesetReference("shadowdark", "1.0.0")));
        Assert.Null(catalog.Resolve(new RulesetReference("shadowdark", "2.0.0")));
    }

    [Fact]
    public void Rejects_unknown_manifest_properties()
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, "invalid"));
        File.WriteAllText(Path.Combine(directory.FullName, "manifest.json"), """{"id":"test","version":"1.0.0","name":"Test","script":"do anything"}""");
        Assert.Throws<System.Text.Json.JsonException>(() => new FileRulesCatalog(root));
    }

    [Fact]
    public void Rejects_duplicate_identity_and_version()
    {
        WriteManifest("test", "1.0.0", "First", "first"); WriteManifest("test", "1.0.0", "Second", "second");
        Assert.Throws<InvalidDataException>(() => new FileRulesCatalog(root));
    }

    [Fact]
    public void Loads_types_records_inheritance_and_references()
    {
        var package = WriteManifest("test", "1.0.0", "Test");
        File.WriteAllText(Path.Combine(package.FullName, "record-types.json"), """
        {"recordTypes":[
          {"key":"named","label":"Named","displayField":"name","fields":[{"key":"name","label":"Name","valueKind":"text","cardinality":"one"}]},
          {"key":"ability","label":"Ability","extends":"named","displayField":"name","fields":[]},
          {"key":"ancestry","label":"Ancestry","extends":"named","displayField":"name","fields":[{"key":"abilities","label":"Abilities","valueKind":"rules-reference","recordType":"ability","cardinality":"many"}]}
        ]}
        """);
        File.WriteAllText(Path.Combine(package.FullName, "records.json"), """
        {"records":[
          {"key":"senses","recordType":"ability","values":{"name":"Keen Senses"}},
          {"key":"elf","recordType":"ancestry","values":{"name":"Elf","abilities":[{"recordType":"ability","key":"senses"}]}}
        ]}
        """);

        var descriptor = Assert.Single(new FileRulesCatalog(root).List());
        Assert.True(descriptor.RecordTypes.Accepts("named", "ancestry"));
        Assert.Equal(2, descriptor.RecordTypes.FieldsFor("ancestry").Count);
        Assert.Equal("elf", Assert.Single(descriptor.RecordsOfType("ancestry")).Key);
    }

    [Fact]
    public void Rejects_unresolved_rules_references()
    {
        var package = WriteManifest("test", "1.0.0", "Test");
        File.WriteAllText(Path.Combine(package.FullName, "record-types.json"), """
        {"recordTypes":[
          {"key":"ability","label":"Ability","fields":[]},
          {"key":"ancestry","label":"Ancestry","fields":[{"key":"ability","label":"Ability","valueKind":"rules-reference","recordType":"ability","cardinality":"one"}]}
        ]}
        """);
        File.WriteAllText(Path.Combine(package.FullName, "records.json"), """{"records":[{"key":"elf","recordType":"ancestry","values":{"ability":{"recordType":"ability","key":"missing"}}}]}""");
        Assert.Throws<InvalidDataException>(() => new FileRulesCatalog(root));
    }

    private DirectoryInfo WriteManifest(string id, string version, string name, string directory = "package")
    {
        var path = Directory.CreateDirectory(Path.Combine(root, directory));
        File.WriteAllText(Path.Combine(path.FullName, "manifest.json"), $$"""{"id":"{{id}}","version":"{{version}}","name":"{{name}}"}""");
        return path;
    }
}
