using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Ai;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Tests;

public class AnalysisEndpointsTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        factory.FakeLlm.Respond = _ => new LlmJsonResponse(FakeLlmClient.DefaultAnswer, "fake-model");
        var response = await _client.PutAsJsonAsync("/api/profile",
            new SaveProfileRequest("Senior .NET developer, 12 years in IT, based in Barcelona (EU)."), TestJson.Options);
        response.EnsureSuccessStatusCode();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Analyze_SavesJobDescriptionAndAnalysis()
    {
        var app = await CreateApplicationAsync();

        var response = await AnalyzeAsync(app.Id, "Senior .NET engineer, remote within the EU.");
        var detail = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Senior .NET engineer, remote within the EU.", detail!.JobDescription);
        Assert.NotNull(detail.Analysis);
        Assert.Equal(78, detail.Analysis.MatchScore);
        Assert.Equal("fake-model", detail.Analysis.Model);

        var fetched = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{app.Id}", TestJson.Options);
        Assert.Equal(["C#", "ASP.NET Core", "PostgreSQL"], fetched!.Analysis!.Stack);
    }

    [Fact]
    public async Task Analyze_SendsTheCvSummaryToTheModel()
    {
        var app = await CreateApplicationAsync();

        await AnalyzeAsync(app.Id, "Some post");

        Assert.Contains("based in Barcelona", factory.FakeLlm.LastRequest!.UserPrompt);
    }

    [Fact]
    public async Task Analyze_UsOnlyPost_IsAlwaysRedFlagged()
    {
        var app = await CreateApplicationAsync(); // the fake model returns no red flags at all

        var response = await AnalyzeAsync(app.Id, "Senior C# developer. Fully remote (US only). Must be authorized to work in the United States.");
        var detail = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Contains(detail!.Analysis!.RedFlags, flag => flag.Contains("US"));
    }

    [Fact]
    public async Task Analyze_WithoutText_ReanalysesTheSavedPost()
    {
        var app = await CreateApplicationAsync();
        await AnalyzeAsync(app.Id, "The original saved post");

        var response = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/analysis", new AnalyzeJobRequest(), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The original saved post", factory.FakeLlm.LastRequest!.UserPrompt);
    }

    [Fact]
    public async Task Analyze_NoTextAnywhere_ReturnsValidationProblem()
    {
        var app = await CreateApplicationAsync();

        var response = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/analysis", new AnalyzeJobRequest(), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(LlmFailure.NotConfigured, HttpStatusCode.ServiceUnavailable)]
    [InlineData(LlmFailure.Timeout, HttpStatusCode.GatewayTimeout)]
    [InlineData(LlmFailure.RateLimited, HttpStatusCode.TooManyRequests)]
    [InlineData(LlmFailure.Refused, HttpStatusCode.UnprocessableEntity)]
    [InlineData(LlmFailure.InvalidOutput, HttpStatusCode.BadGateway)]
    [InlineData(LlmFailure.ProviderError, HttpStatusCode.BadGateway)]
    public async Task Analyze_AiFailure_ReturnsProblemButKeepsTheText(LlmFailure failure, HttpStatusCode expected)
    {
        var app = await CreateApplicationAsync();
        factory.FakeLlm.FailWith(failure);

        var response = await AnalyzeAsync(app.Id, "Post that must not be lost");

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var fetched = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{app.Id}", TestJson.Options);
        Assert.Equal("Post that must not be lost", fetched!.JobDescription);
        Assert.Null(fetched.Analysis);
    }

    [Fact]
    public async Task Analyze_InvalidJsonFromModel_ReturnsBadGateway()
    {
        var app = await CreateApplicationAsync();
        factory.FakeLlm.Respond = _ => new LlmJsonResponse("{ this is not json", "fake-model");

        var response = await AnalyzeAsync(app.Id, "Post");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_UnknownApplication_ReturnsNotFound()
    {
        var response = await AnalyzeAsync(Guid.NewGuid(), "Post");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Profile_SaveThenGet_ReturnsTheSummary()
    {
        await _client.PutAsJsonAsync("/api/profile", new SaveProfileRequest("Updated CV summary"), TestJson.Options);

        var profile = await _client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestJson.Options);

        Assert.Equal("Updated CV summary", profile!.CvSummary);
        Assert.NotNull(profile.UpdatedAt);

        // Put the class's standard profile back for the other tests.
        await InitializeAsync();
    }

    private Task<HttpResponseMessage> AnalyzeAsync(Guid id, string jobDescription) =>
        _client.PostAsJsonAsync($"/api/applications/{id}/analysis", new AnalyzeJobRequest(jobDescription), TestJson.Options);

    private async Task<ApplicationDetailResponse> CreateApplicationAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/applications",
            new SaveApplicationRequest($"Company {Guid.NewGuid():N}", "Senior .NET Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent),
            TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options))!;
    }
}

/// <summary>Its own class (own database): no profile exists yet.</summary>
public class AnalysisWithoutProfileTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Analyze_WithoutCvSummary_ReturnsConflictAndDoesNotCallTheModel()
    {
        var created = await _client.PostAsJsonAsync("/api/applications",
            new SaveApplicationRequest("Contoso", "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent), TestJson.Options);
        var app = (await created.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options))!;

        var response = await _client.PostAsJsonAsync($"/api/applications/{app.Id}/analysis", new AnalyzeJobRequest("A post"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Null(factory.FakeLlm.LastRequest);
    }

    [Fact]
    public async Task Profile_Get_WhenNeverSaved_ReturnsEmpty()
    {
        var profile = await _client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestJson.Options);

        Assert.Equal(string.Empty, profile!.CvSummary);
        Assert.Null(profile.UpdatedAt);
    }
}
