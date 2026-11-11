using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Tests;

/// <summary>Analyses written outside the app (Claude via MCP) and the status-only endpoint.</summary>
public class SaveAnalysisAndStatusTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SaveAnalysis_StoresItAndAppliesTheUsOnlyBackstop()
    {
        var app = await CreateApplicationAsync();

        var response = await _client.PutAsJsonAsync($"/api/applications/{app.Id}/analysis",
            new SaveAnalysisRequest(MatchScore: 60, JobDescription: "Remote (US only). C# backend.", Stack: ["C#"], RedFlags: []),
            TestJson.Options);
        var detail = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(60, detail!.Analysis!.MatchScore);
        Assert.Equal("Claude (via MCP)", detail.Analysis.Model);
        Assert.Contains(detail.Analysis.RedFlags, f => f.Contains("US"));
        Assert.Equal("Remote (US only). C# backend.", detail.JobDescription);
    }

    [Fact]
    public async Task SaveAnalysis_ScoreOutOfRange_ReturnsBadRequest()
    {
        var app = await CreateApplicationAsync();

        var response = await _client.PutAsJsonAsync($"/api/applications/{app.Id}/analysis",
            new SaveAnalysisRequest(MatchScore: 150, JobDescription: "Post"), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SaveAnalysis_WithoutAnyJobPost_ReturnsValidationProblem()
    {
        var app = await CreateApplicationAsync();

        var response = await _client.PutAsJsonAsync($"/api/applications/{app.Id}/analysis",
            new SaveAnalysisRequest(MatchScore: 50), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatusAndKeepsTheFurthestStageRule()
    {
        var app = await CreateApplicationAsync();

        await _client.PutAsJsonAsync($"/api/applications/{app.Id}/status", new UpdateStatusRequest(ApplicationStatus.TechInterview), TestJson.Options);
        var response = await _client.PutAsJsonAsync($"/api/applications/{app.Id}/status", new UpdateStatusRequest(ApplicationStatus.Rejected), TestJson.Options);
        var detail = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Equal(ApplicationStatus.Rejected, detail!.Status);
        Assert.Equal(ApplicationStatus.TechInterview, detail.FurthestStage);
    }

    private async Task<ApplicationDetailResponse> CreateApplicationAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/applications",
            new SaveApplicationRequest($"Company {Guid.NewGuid():N}", "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent),
            TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options))!;
    }
}
