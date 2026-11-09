using System.Text.Json;
using System.Text.Json.Serialization;

namespace JobTracker.Api.Tests;

/// <summary>Mirrors the API's JSON settings so tests can read enum-as-string responses.</summary>
internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
