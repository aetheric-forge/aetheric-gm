using System.Text.Json;
using AethericGm.Core.Rules;
using AethericGm.Core.Rules.CharacterSheets;
using AethericGm.Core.Rules.Records;

namespace AethericGm.Core.Characters;

public sealed class CharacterSheetValueValidator(RulesetDescriptor ruleset, CharacterSheetDefinition sheet)
{
    public IReadOnlyList<string> Validate(IReadOnlyDictionary<string, JsonElement> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var diagnostics = new List<string>();
        var fields = sheet.Sections.SelectMany(section => section.Fields).ToArray();
        foreach (var unknown in values.Keys.Except(fields.Select(field => field.Key), StringComparer.Ordinal))
            diagnostics.Add($"Unknown character field '{unknown}' is preserved but cannot be edited with this definition.");
        foreach (var field in fields) ValidateField(field.Key, field.ValueKind, field.Cardinality, field.RecordType, values, diagnostics);
        return diagnostics;
    }

    private void ValidateField(string path, RecordValueKind kind, RecordCardinality cardinality, string? recordType,
        IReadOnlyDictionary<string, JsonElement> values, List<string> diagnostics)
    {
        var key = path.Split('.').Last();
        if (!values.TryGetValue(key, out var value))
        {
            if (cardinality is RecordCardinality.One or RecordCardinality.OneOrMore) diagnostics.Add($"{path} is required.");
            return;
        }
        if (cardinality is RecordCardinality.Many or RecordCardinality.OneOrMore)
        {
            if (value.ValueKind != JsonValueKind.Array) { diagnostics.Add($"{path} must contain a list of values."); return; }
            if (cardinality == RecordCardinality.OneOrMore && value.GetArrayLength() == 0) diagnostics.Add($"{path} requires at least one value.");
            var index = 0; foreach (var item in value.EnumerateArray()) ValidateValue($"{path}[{index++}]", kind, recordType, item, diagnostics);
        }
        else ValidateValue(path, kind, recordType, value, diagnostics);
    }

    private void ValidateValue(string path, RecordValueKind kind, string? expectedType, JsonElement value, List<string> diagnostics)
    {
        switch (kind)
        {
            case RecordValueKind.Text when value.ValueKind != JsonValueKind.String: diagnostics.Add($"{path} must be text."); break;
            case RecordValueKind.Integer when value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out _): diagnostics.Add($"{path} must be an integer."); break;
            case RecordValueKind.Boolean when value.ValueKind is not JsonValueKind.True and not JsonValueKind.False: diagnostics.Add($"{path} must be Yes or No."); break;
            case RecordValueKind.RulesReference: ValidateReference(path, expectedType!, value, diagnostics); break;
            case RecordValueKind.CharacterReference: if (!IsReference(value)) diagnostics.Add($"{path} is not a valid character reference."); break;
            case RecordValueKind.Record: ValidateRecord(path, expectedType!, value, diagnostics); break;
        }
    }

    private void ValidateReference(string path, string expectedType, JsonElement value, List<string> diagnostics)
    {
        if (!IsReference(value)) { diagnostics.Add($"{path} is not a valid rules reference."); return; }
        var actualType = value.GetProperty("recordType").GetString()!; var key = value.GetProperty("key").GetString()!;
        if (!ruleset.RecordTypes.Accepts(expectedType, actualType) || ruleset.Records.All(record => record.RecordType != actualType || record.Key != key))
            diagnostics.Add($"{path} cannot resolve '{actualType}/{key}'. The stored reference has been preserved.");
    }

    private void ValidateRecord(string path, string expectedType, JsonElement value, List<string> diagnostics)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("recordType", out var typeElement) || !value.TryGetProperty("values", out var nested) || nested.ValueKind != JsonValueKind.Object)
        { diagnostics.Add($"{path} is not a valid embedded record."); return; }
        var actualType = typeElement.GetString();
        if (actualType is null || !ruleset.RecordTypes.Accepts(expectedType, actualType)) { diagnostics.Add($"{path} has incompatible record type '{actualType}'."); return; }
        var nestedValues = nested.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal);
        var fields = ruleset.RecordTypes.FieldsFor(actualType);
        foreach (var unknown in nestedValues.Keys.Except(fields.Select(field => field.Key), StringComparer.Ordinal)) diagnostics.Add($"Unknown field '{path}.{unknown}' is preserved but cannot be edited.");
        foreach (var field in fields) ValidateField($"{path}.{field.Key}", field.ValueKind, field.Cardinality, field.RecordType, nestedValues, diagnostics);
    }

    private static bool IsReference(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty("recordType", out var type) && type.ValueKind == JsonValueKind.String &&
        value.TryGetProperty("key", out var key) && key.ValueKind == JsonValueKind.String;
}
