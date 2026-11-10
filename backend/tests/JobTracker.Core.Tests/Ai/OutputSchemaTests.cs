using System.Text.Json;
using JobTracker.Core.Ai;

namespace JobTracker.Core.Tests.Ai;

/// <summary>
/// The provider rejects (HTTP 400) schemas outside the structured-output subset. Catch that here, not in a live call.
/// </summary>
public class OutputSchemaTests
{
    private static readonly string[] UnsupportedKeywords =
        ["minimum", "maximum", "exclusiveMinimum", "exclusiveMaximum", "multipleOf", "minLength", "maxLength", "pattern"];

    [Fact]
    public void OutputSchema_UsesOnlySupportedJsonSchemaFeatures()
    {
        using var document = JsonDocument.Parse(JobAnalyzer.OutputSchema);
        var problems = new List<string>();

        Walk(document.RootElement, "$", problems);

        Assert.Empty(problems);
    }

    private static void Walk(JsonElement node, string path, List<string> problems)
    {
        if (node.ValueKind != JsonValueKind.Object) return;

        if (node.TryGetProperty("type", out var type))
        {
            if (type.ValueKind == JsonValueKind.Array)
                problems.Add($"{path}: type arrays are not supported, use anyOf");
            else if (type.GetString() == "object"
                     && (!node.TryGetProperty("additionalProperties", out var extra) || extra.ValueKind != JsonValueKind.False))
                problems.Add($"{path}: objects need \"additionalProperties\": false");
        }

        foreach (var keyword in UnsupportedKeywords)
        {
            if (node.TryGetProperty(keyword, out _)) problems.Add($"{path}: \"{keyword}\" is not supported");
        }

        foreach (var property in node.EnumerateObject())
        {
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    if (property.Name == "properties")
                        foreach (var field in property.Value.EnumerateObject()) Walk(field.Value, $"{path}.{field.Name}", problems);
                    else
                        Walk(property.Value, $"{path}.{property.Name}", problems);
                    break;
                case JsonValueKind.Array:
                    var i = 0;
                    foreach (var item in property.Value.EnumerateArray()) Walk(item, $"{path}.{property.Name}[{i++}]", problems);
                    break;
            }
        }
    }
}
