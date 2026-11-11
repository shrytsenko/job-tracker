using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Tests;

/// <summary>Every free-text field has a maximum length, so no request can store unbounded text.</summary>
public class InputLimitTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ApplicationNotes_OverTheLimit_ReturnsValidationProblem()
    {
        var request = new SaveApplicationRequest("Contoso", "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent,
            Notes: new string('x', Application.MaxNotesLength + 1));

        var response = await _client.PostAsJsonAsync("/api/applications", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problem!.Errors.Keys, key => key.Equals("Notes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplicationNotes_AtTheLimit_AreSaved()
    {
        var request = new SaveApplicationRequest("Contoso", "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent,
            Notes: new string('x', Application.MaxNotesLength));

        var response = await _client.PostAsJsonAsync("/api/applications", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ContactNotes_OverTheLimit_ReturnsValidationProblem()
    {
        var request = new SaveContactRequest("Dana", ContactRole.Recruiter, Notes: new string('x', Contact.MaxNotesLength + 1));

        var response = await _client.PostAsJsonAsync("/api/contacts", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
