using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol;

namespace JobTracker.Mcp;

/// <summary>
/// The MCP server's only way into the tracker: the same REST API the web UI uses. The MCP process holds no
/// database credentials, and every change goes through the API's validation and domain rules.
/// Responses are returned as JSON text, which Claude reads directly.
/// </summary>
public sealed class TrackerApi(HttpClient http)
{
    public Task<string> GetAsync(string path, CancellationToken ct) => SendAsync(HttpMethod.Get, path, body: null, ct);

    public Task<string> PostAsync(string path, object body, CancellationToken ct) => SendAsync(HttpMethod.Post, path, body, ct);

    public Task<string> PutAsync(string path, object body, CancellationToken ct) => SendAsync(HttpMethod.Put, path, body, ct);

    /// <summary>Deserialises a response (used where a tool combines two API calls).</summary>
    public static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private async Task<string> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new McpException(
                $"Can't reach the Job Tracker API at {http.BaseAddress}. Start it (docker compose up -d) or set JobTrackerApi__BaseUrl.", ex);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                return text;
            }

            // Turn the API's ProblemDetails into a message Claude can act on (which field, what's wrong).
            throw new McpException(DescribeProblem((int)response.StatusCode, text));
        }
    }

    internal static string DescribeProblem(int status, string body)
    {
        try
        {
            var problem = Parse(body);
            var title = problem.TryGetProperty("title", out var t) ? t.GetString() : null;
            var detail = problem.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var errors = problem.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object
                ? string.Join("; ", e.EnumerateObject().Select(p => $"{p.Name}: {string.Join(" ", p.Value.EnumerateArray().Select(m => m.GetString()))}"))
                : null;

            return string.Join(" ", new[] { $"API error {status}.", title, detail, errors }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
        catch (JsonException)
        {
            return status == 404 ? "API error 404: not found. Check the id (use list_applications to find it)." : $"API error {status}.";
        }
    }
}
