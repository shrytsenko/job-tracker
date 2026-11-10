using JobTracker.Core.Ai;

namespace JobTracker.Api.Tests;

/// <summary>Stand-in for the real AI client: answers with whatever <see cref="Respond"/> returns and records each request.</summary>
public sealed class FakeLlmClient : ILlmClient
{
    public const string DefaultAnswer =
        """
        {
          "stack": ["C#", "ASP.NET Core", "PostgreSQL"],
          "seniority": "Senior",
          "locationRestrictions": "Remote EU",
          "employmentType": "Permanent",
          "salary": null,
          "matchScore": 78,
          "strengths": ["Long C# backend experience"],
          "gaps": ["No Kafka"],
          "redFlags": []
        }
        """;

    public Func<LlmJsonRequest, LlmJsonResponse> Respond { get; set; } = _ => new LlmJsonResponse(DefaultAnswer, "fake-model");

    public LlmJsonRequest? LastRequest { get; private set; }

    public void FailWith(LlmFailure failure) =>
        Respond = _ => throw new LlmException(failure, $"Simulated failure: {failure}");

    public Task<LlmJsonResponse> GenerateJsonAsync(LlmJsonRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(Respond(request));
    }
}
