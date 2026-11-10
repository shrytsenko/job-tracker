namespace JobTracker.Core.Ai;

/// <param name="SystemPrompt">Instructions: who the model is and the rules to follow.</param>
/// <param name="UserPrompt">The input to work on.</param>
/// <param name="JsonSchema">JSON Schema the answer must match (the provider enforces it where it can).</param>
public sealed record LlmJsonRequest(string SystemPrompt, string UserPrompt, string JsonSchema);

/// <param name="Json">The model's answer, expected to match the request's schema (still validated by the caller).</param>
/// <param name="Model">The model that actually answered (may be a fallback model).</param>
public sealed record LlmJsonResponse(string Json, string Model);

/// <summary>
/// The application's only view of an LLM provider. Core owns the prompts, schemas and validation;
/// an implementation only turns a request into the provider's API call. Tests use a fake.
/// </summary>
public interface ILlmClient
{
    /// <exception cref="LlmException">The provider could not produce an answer (see <see cref="LlmFailure"/>).</exception>
    Task<LlmJsonResponse> GenerateJsonAsync(LlmJsonRequest request, CancellationToken cancellationToken);
}
