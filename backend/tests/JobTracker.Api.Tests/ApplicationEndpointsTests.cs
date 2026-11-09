using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Tests;

// One factory (one in-memory database) per test class. Tests don't depend on each other:
// each creates its own data with unique names and asserts only on what it created.
public class ApplicationEndpointsTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ThenGetById_ReturnsSavedApplication()
    {
        var request = NewRequest() with { RemoteRegion = "Remote EU", CompensationAmount = 60000, CompensationCurrency = Currency.EUR };

        var createResponse = await _client.PostAsJsonAsync("/api/applications", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var fetched = await _client.GetFromJsonAsync<ApplicationDetailResponse>(createResponse.Headers.Location, TestJson.Options);
        Assert.NotNull(fetched);
        Assert.Equal(request.Company, fetched.Company);
        Assert.Equal(ApplicationStatus.Interested, fetched.Status);
        Assert.Equal("Remote EU", fetched.RemoteRegion);
        Assert.Equal(Currency.EUR, fetched.CompensationCurrency);
    }

    [Fact]
    public async Task Create_WithStatusApplied_SetsAppliedOnToToday()
    {
        var created = await CreateAsync(NewRequest() with { Status = ApplicationStatus.Applied });

        Assert.Equal(ApplicationStatus.Applied, created.Status);
        Assert.Equal(DateOnly.FromDateTime(DateTime.Now), created.AppliedOn);
    }

    [Fact]
    public async Task Create_WithMissingCompany_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/applications", NewRequest() with { Company = "" }, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(problem.Errors.Keys, key => key.Equals("Company", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_WithUnknownEnumValue_ReturnsBadRequest()
    {
        var body = new { company = "X", roleTitle = "Y", track = "Astronaut", channel = "LinkedIn", employmentType = "Permanent" };

        var response = await _client.PostAsJsonAsync("/api/applications", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnsupportedCurrency_ReturnsBadRequest()
    {
        var body = new { company = "X", roleTitle = "Y", track = "Developer", channel = "LinkedIn", employmentType = "Permanent", compensationCurrency = "XYZ" };

        var response = await _client.PostAsJsonAsync("/api/applications", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNotFoundProblemDetails()
    {
        var response = await _client.GetAsync($"/api/applications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task List_FilteredByStatusTrackAndChannel_ReturnsOnlyMatching()
    {
        var match = await CreateAsync(NewRequest() with { Track = Track.SDET, Channel = Channel.Djinni, Status = ApplicationStatus.TechInterview });
        var wrongTrack = await CreateAsync(NewRequest() with { Track = Track.Developer, Channel = Channel.Djinni, Status = ApplicationStatus.TechInterview });
        var wrongStatus = await CreateAsync(NewRequest() with { Track = Track.SDET, Channel = Channel.Djinni, Status = ApplicationStatus.Rejected });

        var results = await _client.GetFromJsonAsync<List<ApplicationSummaryResponse>>(
            "/api/applications?status=TechInterview&track=SDET&channel=Djinni", TestJson.Options);

        Assert.NotNull(results);
        var ids = results.Select(a => a.Id).ToList();
        Assert.Contains(match.Id, ids);
        Assert.DoesNotContain(wrongTrack.Id, ids);
        Assert.DoesNotContain(wrongStatus.Id, ids);
        Assert.All(results, a => Assert.Equal(ApplicationStatus.TechInterview, a.Status));
    }

    [Fact]
    public async Task List_WithSearch_MatchesCompanyOrRoleCaseInsensitively()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var byCompany = await CreateAsync(NewRequest() with { Company = $"Zeta {marker} Systems" });
        var byRole = await CreateAsync(NewRequest() with { RoleTitle = $"Engineer {marker}" });
        var other = await CreateAsync(NewRequest());

        var results = await _client.GetFromJsonAsync<List<ApplicationSummaryResponse>>(
            $"/api/applications?search={marker.ToUpperInvariant()}", TestJson.Options);

        var ids = results!.Select(a => a.Id).ToList();
        Assert.Contains(byCompany.Id, ids);
        Assert.Contains(byRole.Id, ids);
        Assert.DoesNotContain(other.Id, ids);
    }

    [Fact]
    public async Task FollowUps_ReturnsStaleAndDueApplications_ButNotFreshOrClosedOnes()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var stale = await CreateAsync(NewRequest() with { Status = ApplicationStatus.Applied, LastContactOn = today.AddDays(-6) });
        var due = await CreateAsync(NewRequest() with { NextActionDescription = "Send CV", NextActionDueOn = today });
        var fresh = await CreateAsync(NewRequest() with { Status = ApplicationStatus.Applied, LastContactOn = today.AddDays(-1) });
        var closed = await CreateAsync(NewRequest() with { Status = ApplicationStatus.Rejected, NextActionDescription = "Old task", NextActionDueOn = today.AddDays(-3) });

        var results = await _client.GetFromJsonAsync<List<FollowUpResponse>>("/api/applications/follow-ups", TestJson.Options);

        var byId = results!.ToDictionary(f => f.Application.Id);
        Assert.True(byId[stale.Id].IsStale);
        Assert.Equal(6, byId[stale.Id].DaysSinceContact);
        Assert.True(byId[due.Id].IsNextActionDue);
        Assert.DoesNotContain(fresh.Id, byId.Keys);
        Assert.DoesNotContain(closed.Id, byId.Keys);
    }

    [Fact]
    public async Task FollowUps_CustomDays_ChangesStaleThreshold()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var threeDaysQuiet = await CreateAsync(NewRequest() with { Status = ApplicationStatus.TechInterview, LastContactOn = today.AddDays(-3) });

        var withDefault = await _client.GetFromJsonAsync<List<FollowUpResponse>>("/api/applications/follow-ups", TestJson.Options);
        var withTwoDays = await _client.GetFromJsonAsync<List<FollowUpResponse>>("/api/applications/follow-ups?days=2", TestJson.Options);

        Assert.DoesNotContain(withDefault!, f => f.Application.Id == threeDaysQuiet.Id);
        Assert.Contains(withTwoDays!, f => f.Application.Id == threeDaysQuiet.Id);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("366")]
    [InlineData("abc")]
    public async Task FollowUps_InvalidDays_ReturnsBadRequest(string days)
    {
        var response = await _client.GetAsync($"/api/applications/follow-ups?days={days}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_WithInvalidFilterValue_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/applications?status=NotAStatus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesFieldsAndStatus()
    {
        var created = await CreateAsync(NewRequest());
        var update = NewRequest() with
        {
            Company = created.Company,
            RoleTitle = "Lead .NET Engineer",
            Status = ApplicationStatus.RecruiterScreen,
            NextActionDescription = "Prepare for screen",
            NextActionDueOn = new DateOnly(2026, 12, 1),
        };

        var response = await _client.PutAsJsonAsync($"/api/applications/{created.Id}", update, TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{created.Id}", TestJson.Options);
        Assert.NotNull(fetched);
        Assert.Equal("Lead .NET Engineer", fetched.RoleTitle);
        Assert.Equal(ApplicationStatus.RecruiterScreen, fetched.Status);
        Assert.Equal("Prepare for screen", fetched.NextActionDescription);
        Assert.Equal(new DateOnly(2026, 12, 1), fetched.NextActionDueOn);
    }

    [Fact]
    public async Task Update_ToRejected_KeepsFurthestStageReached()
    {
        var created = await CreateAsync(NewRequest() with { Status = ApplicationStatus.TechInterview });

        var response = await _client.PutAsJsonAsync($"/api/applications/{created.Id}",
            NewRequest() with { Company = created.Company, Status = ApplicationStatus.Rejected }, TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Equal(ApplicationStatus.Rejected, updated!.Status);
        Assert.Equal(ApplicationStatus.TechInterview, updated.FurthestStage);
    }

    [Fact]
    public async Task CvVersion_IsSavedAndSuggestedOnce()
    {
        var marker = Guid.NewGuid().ToString("N")[..6];
        var cv = $"Developer CV {marker}";
        var created = await CreateAsync(NewRequest() with { CvVersion = cv });
        await CreateAsync(NewRequest() with { CvVersion = cv.ToUpperInvariant() });

        var fetched = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{created.Id}", TestJson.Options);
        var suggestions = await _client.GetFromJsonAsync<List<string>>("/api/applications/cv-versions");

        Assert.Equal(cv, fetched!.CvVersion);
        Assert.Single(suggestions!, s => s.Equals(cv, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Update_Unknown_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync($"/api/applications/{Guid.NewGuid()}", NewRequest(), TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesApplication()
    {
        var created = await CreateAsync(NewRequest());
        await _client.PostAsJsonAsync($"/api/applications/{created.Id}/activities",
            new AddActivityRequest(ActivityType.Note, "will be deleted too"), TestJson.Options);

        var deleteResponse = await _client.DeleteAsync($"/api/applications/{created.Id}");
        var getResponse = await _client.GetAsync($"/api/applications/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task AddActivity_AppearsInTimelineAndUpdatesLastContact()
    {
        var created = await CreateAsync(NewRequest());
        var date = new DateOnly(2026, 11, 5);

        var response = await _client.PostAsJsonAsync($"/api/applications/{created.Id}/activities",
            new AddActivityRequest(ActivityType.ReplyReceived, "Recruiter replied", date), TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{created.Id}", TestJson.Options);
        Assert.NotNull(fetched);
        var activity = Assert.Single(fetched.Activities);
        Assert.Equal("Recruiter replied", activity.Text);
        Assert.Equal(date, fetched.LastContactOn);
    }

    [Fact]
    public async Task AddActivity_WithEmptyText_ReturnsValidationProblem()
    {
        var created = await CreateAsync(NewRequest());

        var response = await _client.PostAsJsonAsync($"/api/applications/{created.Id}/activities",
            new AddActivityRequest(ActivityType.Note, ""), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SetNextAction_UpdatesAndClears()
    {
        var created = await CreateAsync(NewRequest());
        var due = new DateOnly(2026, 12, 1);

        var setResponse = await _client.PutAsJsonAsync($"/api/applications/{created.Id}/next-action",
            new SetNextActionRequest("Message Dana", due), TestJson.Options);
        var afterSet = await setResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);
        Assert.Equal("Message Dana", afterSet!.NextActionDescription);
        Assert.Equal(due, afterSet.NextActionDueOn);

        var clearResponse = await _client.PutAsJsonAsync($"/api/applications/{created.Id}/next-action",
            new SetNextActionRequest(null), TestJson.Options);
        var afterClear = await clearResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Null(afterClear!.NextActionDescription);
        Assert.Null(afterClear.NextActionDueOn);
    }

    [Fact]
    public async Task SetNextAction_DueDateWithoutDescription_ReturnsBadRequest()
    {
        var created = await CreateAsync(NewRequest());

        var response = await _client.PutAsJsonAsync($"/api/applications/{created.Id}/next-action",
            new SetNextActionRequest("", new DateOnly(2026, 12, 1)), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteNextAction_LogsActivityClearsActionAndCountsAsContact()
    {
        var created = await CreateAsync(NewRequest() with { NextActionDescription = "Message Dana", NextActionDueOn = new DateOnly(2026, 10, 26) });
        var date = new DateOnly(2026, 11, 9);

        var response = await _client.PostAsJsonAsync($"/api/applications/{created.Id}/next-action/complete",
            new CompleteNextActionRequest(ActivityType.MessageSent, date), TestJson.Options);
        var completed = await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(completed!.NextActionDescription);
        Assert.Null(completed.NextActionDueOn);
        Assert.Equal(date, completed.LastContactOn);
        var activity = Assert.Single(completed.Activities);
        Assert.Equal("Done: Message Dana", activity.Text);
        Assert.Equal(ActivityType.MessageSent, activity.Type);
    }

    [Fact]
    public async Task CompleteNextAction_WhenNoneSet_ReturnsConflict()
    {
        var created = await CreateAsync(NewRequest());

        var response = await _client.PostAsJsonAsync($"/api/applications/{created.Id}/next-action/complete",
            new CompleteNextActionRequest(), TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task LinkAndUnlinkContact_UpdatesApplicationContacts()
    {
        var application = await CreateAsync(NewRequest());
        var contactResponse = await _client.PostAsJsonAsync("/api/contacts",
            new SaveContactRequest("Jane Recruiter", ContactRole.Recruiter, Company: "Fabrikam"), TestJson.Options);
        var contact = (await contactResponse.Content.ReadFromJsonAsync<ContactDetailResponse>(TestJson.Options))!;

        var link = await _client.PutAsync($"/api/applications/{application.Id}/contacts/{contact.Id}", null);
        var linkAgain = await _client.PutAsync($"/api/applications/{application.Id}/contacts/{contact.Id}", null);
        var linked = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{application.Id}", TestJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, link.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, linkAgain.StatusCode);
        Assert.Equal(contact.Id, Assert.Single(linked!.Contacts).Id);

        var unlink = await _client.DeleteAsync($"/api/applications/{application.Id}/contacts/{contact.Id}");
        var unlinked = await _client.GetFromJsonAsync<ApplicationDetailResponse>($"/api/applications/{application.Id}", TestJson.Options);

        Assert.Equal(HttpStatusCode.NoContent, unlink.StatusCode);
        Assert.Empty(unlinked!.Contacts);
    }

    [Fact]
    public async Task LinkContact_UnknownContact_ReturnsNotFound()
    {
        var application = await CreateAsync(NewRequest());

        var response = await _client.PutAsync($"/api/applications/{application.Id}/contacts/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static SaveApplicationRequest NewRequest() => new(
        Company: $"Company {Guid.NewGuid():N}",
        RoleTitle: "Senior .NET Developer",
        Track: Track.Developer,
        Channel: Channel.LinkedIn,
        EmploymentType: EmploymentType.Permanent);

    private async Task<ApplicationDetailResponse> CreateAsync(SaveApplicationRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/applications", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationDetailResponse>(TestJson.Options))!;
    }
}
