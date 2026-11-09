using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Tests;

public class ContactEndpointsTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_ThenGetById_ReturnsSavedContact()
    {
        var request = new SaveContactRequest("Alex Hiring", ContactRole.HiringManager, "Northwind", Email: "alex@example.com");

        var response = await _client.PostAsJsonAsync("/api/contacts", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var fetched = await _client.GetFromJsonAsync<ContactDetailResponse>(response.Headers.Location, TestJson.Options);
        Assert.NotNull(fetched);
        Assert.Equal("Alex Hiring", fetched.Name);
        Assert.Equal(ContactRole.HiringManager, fetched.Role);
        Assert.Equal("alex@example.com", fetched.Email);
    }

    [Fact]
    public async Task Create_WithInvalidEmail_ReturnsValidationProblem()
    {
        var request = new SaveContactRequest("Bad Email", ContactRole.Recruiter, Email: "not-an-email");

        var response = await _client.PostAsJsonAsync("/api/contacts", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(problem!.Errors.Keys, key => key.Equals("Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Update_ChangesFields()
    {
        var created = await CreateAsync(new SaveContactRequest("Sam Referrer", ContactRole.Referrer));

        var response = await _client.PutAsJsonAsync($"/api/contacts/{created.Id}",
            new SaveContactRequest("Sam Referrer", ContactRole.ExColleague, "Tailspin", Notes: "Worked together 2019"), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ContactDetailResponse>(TestJson.Options);
        Assert.Equal(ContactRole.ExColleague, updated!.Role);
        Assert.Equal("Tailspin", updated.Company);
    }

    [Fact]
    public async Task Delete_RemovesContact()
    {
        var created = await CreateAsync(new SaveContactRequest("Temp Contact", ContactRole.Other));

        var deleteResponse = await _client.DeleteAsync($"/api/contacts/{created.Id}");
        var getResponse = await _client.GetAsync($"/api/contacts/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Unknown_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/contacts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<ContactDetailResponse> CreateAsync(SaveContactRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/contacts", request, TestJson.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ContactDetailResponse>(TestJson.Options))!;
    }
}
