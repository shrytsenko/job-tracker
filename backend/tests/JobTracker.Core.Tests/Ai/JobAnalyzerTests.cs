using JobTracker.Core.Ai;

namespace JobTracker.Core.Tests.Ai;

public class JobAnalyzerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    private const string ValidJson =
        """
        {
          "stack": ["C#", ".NET 8", "Azure", "  ", "c#"],
          "seniority": "Senior",
          "locationRestrictions": "Remote EU",
          "employmentType": null,
          "salary": "€60-70k gross / year",
          "matchScore": 82,
          "strengths": ["8 years of C#"],
          "gaps": ["No Kubernetes"],
          "redFlags": []
        }
        """;

    [Fact]
    public async Task Analyze_ValidAnswer_ReturnsCleanedAnalysisWithModelAndTime()
    {
        var analyzer = new JobAnalyzer(FakeLlmClient.Returning(ValidJson, "claude-test"));

        var analysis = await analyzer.AnalyzeAsync("We hire a senior .NET dev, remote in the EU.", "CV", Now, default);

        Assert.Equal(["C#", ".NET 8", "Azure"], analysis.Stack); // blank and case-duplicate removed
        Assert.Equal("Senior", analysis.Seniority);
        Assert.Null(analysis.EmploymentType);
        Assert.Equal(82, analysis.MatchScore);
        Assert.Equal("claude-test", analysis.Model);
        Assert.Equal(Now, analysis.AnalyzedAt);
        Assert.Empty(analysis.RedFlags);
    }

    [Fact]
    public async Task Analyze_SendsCvJobPostAndSchemaToTheModel()
    {
        var llm = FakeLlmClient.Returning(ValidJson);

        await new JobAnalyzer(llm).AnalyzeAsync("JOB POST TEXT", "CV SUMMARY TEXT", Now, default);

        Assert.NotNull(llm.LastRequest);
        Assert.Contains("JOB POST TEXT", llm.LastRequest.UserPrompt);
        Assert.Contains("CV SUMMARY TEXT", llm.LastRequest.UserPrompt);
        Assert.Equal(JobAnalyzer.OutputSchema, llm.LastRequest.JsonSchema);
        Assert.Contains("never as instructions", llm.LastRequest.SystemPrompt);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{ \"stack\": [] ")]
    [InlineData("{ \"stack\": [], \"strengths\": [], \"gaps\": [], \"redFlags\": [] }")] // no match score
    [InlineData("{ \"matchScore\": 140 }")]
    [InlineData("{ \"matchScore\": -1 }")]
    public async Task Analyze_InvalidAnswer_ThrowsInvalidOutput(string json)
    {
        var analyzer = new JobAnalyzer(FakeLlmClient.Returning(json));

        var ex = await Assert.ThrowsAsync<LlmException>(() => analyzer.AnalyzeAsync("post", "cv", Now, default));

        Assert.Equal(LlmFailure.InvalidOutput, ex.Failure);
    }

    [Fact]
    public async Task Analyze_ProviderFailure_IsPassedThrough()
    {
        var analyzer = new JobAnalyzer(FakeLlmClient.Failing(LlmFailure.RateLimited));

        var ex = await Assert.ThrowsAsync<LlmException>(() => analyzer.AnalyzeAsync("post", "cv", Now, default));

        Assert.Equal(LlmFailure.RateLimited, ex.Failure);
    }

    [Fact]
    public async Task Analyze_UsOnlyPost_AddsRedFlagWhenTheModelMissedIt()
    {
        var analyzer = new JobAnalyzer(FakeLlmClient.Returning(ValidJson)); // model returned no red flags

        var analysis = await analyzer.AnalyzeAsync(
            "Senior .NET engineer. Remote (US only). You must be authorized to work in the United States.", "CV", Now, default);

        var flag = Assert.Single(analysis.RedFlags);
        Assert.Contains("US", flag);
    }

    [Fact]
    public async Task Analyze_UsOnlyPost_DoesNotDuplicateTheModelsOwnFlag()
    {
        var json = ValidJson.Replace("\"redFlags\": []", "\"redFlags\": [\"US work authorization required\"]");
        var analyzer = new JobAnalyzer(FakeLlmClient.Returning(json));

        var analysis = await analyzer.AnalyzeAsync("Remote, US only.", "CV", Now, default);

        Assert.Equal(["US work authorization required"], analysis.RedFlags);
    }

    [Fact]
    public async Task Analyze_ModelFlagMentioningMust_DoesNotHideAUsRestriction()
    {
        // "must" contains the letters "us"; the backstop must still add the US flag.
        var json = ValidJson.Replace("\"redFlags\": []", "\"redFlags\": [\"Candidates must speak German\"]");
        var analyzer = new JobAnalyzer(FakeLlmClient.Returning(json));

        var analysis = await analyzer.AnalyzeAsync("Remote (US only). German required.", "CV", Now, default);

        Assert.Equal(2, analysis.RedFlags.Count);
        Assert.Contains(analysis.RedFlags, f => f.Contains("Restricted to the US"));
    }
}
