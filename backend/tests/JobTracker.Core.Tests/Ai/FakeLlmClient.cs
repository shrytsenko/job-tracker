using JobTracker.Core.Ai;

namespace JobTracker.Core.Tests.Ai;

/// <summary>Returns a canned answer (or throws a canned failure) and records the request it received.</summary>
internal sealed class FakeLlmClient(Func<LlmJsonRequest, LlmJsonResponse> respond) : ILlmClient
{
    public LlmJsonRequest? LastRequest { get; private set; }

    public static FakeLlmClient Returning(string json, string model = "fake-model") =>
        new(_ => new LlmJsonResponse(json, model));

    public static FakeLlmClient Failing(LlmFailure failure) =>
        new(_ => throw new LlmException(failure, $"Fake failure: {failure}"));

    public Task<LlmJsonResponse> GenerateJsonAsync(LlmJsonRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(respond(request));
    }
}
