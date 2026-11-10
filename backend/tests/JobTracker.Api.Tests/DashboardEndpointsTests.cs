using System.Net.Http.Json;
using JobTracker.Core.Enums;
using JobTracker.Core.Reporting;
using JobTracker.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace JobTracker.Api.Tests;

/// <summary>
/// The phase 3 "done" check: the dashboard shows the right numbers for the seed data.
/// This class gets its own factory, so its database contains only the demo data.
/// The expected numbers are the table in the DemoData class comment.
/// </summary>
public class DashboardEndpointsTests : IClassFixture<JobTrackerApiFactory>, IAsyncLifetime
{
    private readonly JobTrackerApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardEndpointsTests(JobTrackerApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobTrackerDbContext>();
        await DemoData.SeedIfEmptyAsync(db, new DateOnly(2026, 11, 10));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Dashboard_CountsPerStatus_MatchSeedData()
    {
        var stats = await GetAsync();

        Assert.Equal(20, stats.Total);
        var byStatus = stats.ByStatus.ToDictionary(s => s.Status, s => s.Count);
        Assert.Equal(2, byStatus[ApplicationStatus.Interested]);
        Assert.Equal(5, byStatus[ApplicationStatus.Applied]);
        Assert.Equal(3, byStatus[ApplicationStatus.RecruiterScreen]);
        Assert.Equal(2, byStatus[ApplicationStatus.TechInterview]);
        Assert.Equal(1, byStatus[ApplicationStatus.FinalInterview]);
        Assert.Equal(1, byStatus[ApplicationStatus.Offer]);
        Assert.Equal(0, byStatus[ApplicationStatus.Accepted]);
        Assert.Equal(3, byStatus[ApplicationStatus.Rejected]);
        Assert.Equal(1, byStatus[ApplicationStatus.Withdrawn]);
        Assert.Equal(2, byStatus[ApplicationStatus.NoResponse]);
    }

    [Fact]
    public async Task Dashboard_CountsPerTrack_MatchSeedData()
    {
        var stats = await GetAsync();

        var byTrack = stats.ByTrack.ToDictionary(t => t.Track, t => t.Count);
        Assert.Equal(10, byTrack[Track.Developer]);
        Assert.Equal(8, byTrack[Track.SDET]);
        Assert.Equal(2, byTrack[Track.Integration]);
        Assert.Equal(0, byTrack[Track.Other]);
    }

    [Theory]
    [InlineData(Track.Developer, 10, 9, 6, 4, 1)]
    [InlineData(Track.SDET, 8, 7, 4, 2, 0)]
    [InlineData(Track.Integration, 2, 2, 1, 0, 0)]
    [InlineData(Track.Other, 0, 0, 0, 0, 0)]
    public async Task Dashboard_FunnelPerTrack_MatchesSeedData(Track track, int total, int applied, int screen, int tech, int offer)
    {
        var stats = await GetAsync();

        var funnel = stats.Funnels.Single(f => f.Track == track);
        Assert.Equal(total, funnel.Total);
        Assert.Equal([applied, screen, tech, offer], funnel.Steps.Select(s => s.Reached));
    }

    [Fact]
    public async Task Dashboard_ConversionRates_ForDeveloperTrack()
    {
        var stats = await GetAsync();

        var developer = stats.Funnels.Single(f => f.Track == Track.Developer);
        Assert.Equal(6.0 / 9, developer.Steps[1].ConversionFromPrevious!.Value, precision: 6);
        Assert.Equal(4.0 / 6, developer.Steps[2].ConversionFromPrevious!.Value, precision: 6);
        Assert.Equal(1.0 / 4, developer.Steps[3].ConversionFromPrevious!.Value, precision: 6);
        Assert.Equal(1.0 / 9, developer.OverallConversion!.Value, precision: 6);
    }

    [Fact]
    public async Task SeedIfEmpty_DoesNothingWhenDataExists()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobTrackerDbContext>();

        var seededAgain = await DemoData.SeedIfEmptyAsync(db, new DateOnly(2026, 11, 10));

        Assert.False(seededAgain);
        Assert.Equal(20, (await GetAsync()).Total);
    }

    private async Task<DashboardStats> GetAsync() =>
        (await _client.GetFromJsonAsync<DashboardStats>("/api/dashboard", TestJson.Options))!;
}
