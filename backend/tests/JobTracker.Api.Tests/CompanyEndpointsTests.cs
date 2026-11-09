using System.Net.Http.Json;
using JobTracker.Api.Contracts;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Tests;

public class CompanyEndpointsTests(JobTrackerApiFactory factory) : IClassFixture<JobTrackerApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_CombinesApplicationAndContactCompanies_WithoutCaseDuplicates()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var hiringCompany = $"Contoso {marker}";
        var agency = $"Agency {marker}";

        await PostAsync("/api/applications", new SaveApplicationRequest(hiringCompany, "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent));
        await PostAsync("/api/contacts", new SaveContactRequest("Recruiter One", ContactRole.Recruiter, Company: hiringCompany.ToLowerInvariant()));
        await PostAsync("/api/contacts", new SaveContactRequest("Recruiter Two", ContactRole.Recruiter, Company: agency));
        await PostAsync("/api/contacts", new SaveContactRequest("No Company", ContactRole.Other));

        var companies = await _client.GetFromJsonAsync<List<string>>("/api/companies");

        Assert.NotNull(companies);
        Assert.Single(companies, c => c.Equals(hiringCompany, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(agency, companies);
        Assert.Equal(companies.Order(StringComparer.OrdinalIgnoreCase), companies);
    }

    private async Task PostAsync<T>(string url, T body)
    {
        var response = await _client.PostAsJsonAsync(url, body, TestJson.Options);
        response.EnsureSuccessStatusCode();
    }
}
