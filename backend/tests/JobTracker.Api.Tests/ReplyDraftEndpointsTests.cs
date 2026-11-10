using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Ai;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Tests;

public class ReplyDraftEndpointsTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>, IAsyncLifetime
{
    private const string DraftJson = """{ "subject": "Senior .NET Developer role", "body": "Hi Dana, thanks for reaching out. Alex" }""";

    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        factory.FakeLlm.Respond = _ => new LlmJsonResponse(DraftJson, "fake-model");
        var response = await _client.PutAsJsonAsync("/api/profile",
            new SaveProfileRequest("Senior .NET developer based in Barcelona.", "Alex Doe"), TestJson.Options);
        response.EnsureSuccessStatusCode();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Draft_Email_ReturnsSubjectAndBody()
    {
        var app = await CreateApplicationAsync();

        var response = await DraftAsync(app.Id, new DraftReplyRequest(ReplyChannel.Email, ReplyTone.Short));
        var draft = await response.Content.ReadFromJsonAsync<ReplyDraftResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Senior .NET Developer role", draft!.Subject);
        Assert.StartsWith("Hi Dana", draft.Body);
        Assert.Equal("fake-model", draft.Model);
    }

    [Fact]
    public async Task Draft_LinkedIn_HasNoSubject()
    {
        var app = await CreateApplicationAsync();

        var response = await DraftAsync(app.Id, new DraftReplyRequest(ReplyChannel.LinkedIn));
        var draft = await response.Content.ReadFromJsonAsync<ReplyDraftResponse>(TestJson.Options);

        Assert.Null(draft!.Subject);
    }

    [Fact]
    public async Task Draft_UsesLinkedContactNameAndProfile()
    {
        var app = await CreateApplicationAsync();
        var contact = await CreateContactAsync("Dana Whitfield");
        await _client.PutAsync($"/api/applications/{app.Id}/contacts/{contact.Id}", null);

        await DraftAsync(app.Id, new DraftReplyRequest(ContactId: contact.Id, TheirMessage: "Are you open to a call?", Intent: "Yes, Thursday works"));

        var prompt = factory.FakeLlm.LastRequest!.UserPrompt;
        Assert.Contains("Dana Whitfield", prompt);
        Assert.Contains("Alex Doe", prompt);
        Assert.Contains("Are you open to a call?", prompt);
        Assert.Contains("Thursday works", prompt);
    }

    [Fact]
    public async Task Draft_ContactNotLinkedToApplication_ReturnsValidationProblem()
    {
        var app = await CreateApplicationAsync();
        var stranger = await CreateContactAsync("Not linked");

        var response = await DraftAsync(app.Id, new DraftReplyRequest(ContactId: stranger.Id));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(LlmFailure.NotConfigured, HttpStatusCode.ServiceUnavailable)]
    [InlineData(LlmFailure.NoCredits, HttpStatusCode.PaymentRequired)]
    [InlineData(LlmFailure.InvalidOutput, HttpStatusCode.BadGateway)]
    public async Task Draft_AiFailure_ReturnsProblem(LlmFailure failure, HttpStatusCode expected)
    {
        var app = await CreateApplicationAsync();
        factory.FakeLlm.FailWith(failure);

        var response = await DraftAsync(app.Id, new DraftReplyRequest());

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        factory.FakeLlm.Respond = _ => new LlmJsonResponse(DraftJson, "fake-model");
    }

    [Fact]
    public async Task Draft_UnknownApplication_ReturnsNotFound()
    {
        var response = await DraftAsync(Guid.NewGuid(), new DraftReplyRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Profile_SavesAndReturnsName()
    {
        var profile = await _client.GetFromJsonAsync<ProfileResponse>("/api/profile", TestJson.Options);

        Assert.Equal("Alex Doe", profile!.FullName);
    }

    private Task<HttpResponseMessage> DraftAsync(Guid id, DraftReplyRequest request) =>
        _client.PostAsJsonAsync($"/api/applications/{id}/reply-draft", request, TestJson.Options);

    private async Task<ApplicationDetailResponse> CreateApplicationAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/applications",
            new SaveApplicationRequest($"Company {Guid.NewGuid():N}", "Senior .NET Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent),
            TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options))!;
    }

    private async Task<ContactDetailResponse> CreateContactAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", new SaveContactRequest(name, ContactRole.Recruiter), TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContactDetailResponse>(TestJson.Options))!;
    }
}

/// <summary>Drafts work without any profile (own database: no profile saved).</summary>
public class ReplyDraftWithoutProfileTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    [Fact]
    public async Task Draft_WithoutProfile_StillWorks()
    {
        var client = factory.CreateClient();
        factory.FakeLlm.Respond = _ => new LlmJsonResponse("""{ "subject": null, "body": "Hi, thanks. [Your name]" }""", "fake-model");
        var created = await client.PostAsJsonAsync("/api/applications",
            new SaveApplicationRequest("Contoso", "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent), TestJson.Options);
        var app = (await created.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options))!;

        var response = await client.PostAsJsonAsync($"/api/applications/{app.Id}/reply-draft", new DraftReplyRequest(), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("<candidate_cv_summary>", factory.FakeLlm.LastRequest!.UserPrompt);
    }
}
