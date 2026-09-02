using AethericGm.Core.Rules.Records;
using System.Text.Json;

namespace AethericGm.Tests;

public sealed class RecordTypeRegistryTests
{
    [Fact]
    public void Serializes_rules_record_references_using_package_property_names()
    {
        var reference = JsonSerializer.SerializeToElement(new RulesRecordReference("ability", "farsight"));

        Assert.Equal("ability", reference.GetProperty("recordType").GetString());
        Assert.Equal("farsight", reference.GetProperty("key").GetString());
        Assert.False(reference.TryGetProperty("RecordType", out _));
        Assert.False(reference.TryGetProperty("Key", out _));
    }

    [Fact]
    public void Resolves_inherited_fields_and_subtype_compatibility()
    {
        var registry = new RecordTypeRegistry([
            new RecordTypeDefinition("named-record", "Named record", [new RecordFieldDefinition("name", "Name", RecordValueKind.Text)], displayField: "name"),
            new RecordTypeDefinition("ability", "Ability", [new RecordFieldDefinition("description", "Description", RecordValueKind.Text)], extends: "named-record", displayField: "name")
        ]);

        Assert.Equal(["name", "description"], registry.FieldsFor("ability").Select(field => field.Key));
        Assert.True(registry.Accepts("named-record", "ability"));
        Assert.False(registry.Accepts("ability", "named-record"));
    }

    [Fact]
    public void Rejects_cycles_unknown_targets_and_inherited_field_replacement()
    {
        Assert.Throws<ArgumentException>(() => new RecordTypeRegistry([
            new RecordTypeDefinition("first", "First", [], extends: "second"),
            new RecordTypeDefinition("second", "Second", [], extends: "first")
        ]));
        Assert.Throws<ArgumentException>(() => new RecordTypeRegistry([
            new RecordTypeDefinition("holder", "Holder", [new RecordFieldDefinition("child", "Child", RecordValueKind.Record, recordType: "missing")])
        ]));
        Assert.Throws<ArgumentException>(() => new RecordTypeRegistry([
            new RecordTypeDefinition("parent", "Parent", [new RecordFieldDefinition("name", "Name", RecordValueKind.Text)]),
            new RecordTypeDefinition("child", "Child", [new RecordFieldDefinition("name", "Name", RecordValueKind.Integer)], extends: "parent")
        ]));
    }
}
