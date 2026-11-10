using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Reporting;

namespace JobTracker.Core.Tests.Reporting;

public class DashboardCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 11, 10);

    [Fact]
    public void Calculate_NoApplications_ReturnsZerosAndNoConversions()
    {
        var stats = DashboardCalculator.Calculate([]);

        Assert.Equal(0, stats.Total);
        Assert.All(stats.ByStatus, s => Assert.Equal(0, s.Count));
        Assert.All(stats.Funnels, f =>
        {
            Assert.All(f.Steps, step => Assert.Equal(0, step.Reached));
            Assert.All(f.Steps, step => Assert.Null(step.ConversionFromPrevious));
            Assert.Null(f.OverallConversion);
        });
    }

    [Fact]
    public void Calculate_ListsEveryStatusAndTrack_IncludingZeros()
    {
        var stats = DashboardCalculator.Calculate([Create(Track.Developer, ApplicationStatus.Applied)]);

        Assert.Equal(Enum.GetValues<ApplicationStatus>().Length, stats.ByStatus.Count);
        Assert.Equal(Enum.GetValues<Track>().Length, stats.ByTrack.Count);
        Assert.Equal(1, stats.ByStatus.Single(s => s.Status == ApplicationStatus.Applied).Count);
        Assert.Equal(0, stats.ByStatus.Single(s => s.Status == ApplicationStatus.Offer).Count);
    }

    [Fact]
    public void Funnel_CountsFurthestStageReached_NotCurrentStatus()
    {
        // Rejected after the tech interview: still counts towards Applied, Screen and Tech.
        var rejected = Create(Track.Developer, ApplicationStatus.TechInterview, ApplicationStatus.Rejected);

        var steps = FunnelFor(Track.Developer, rejected);

        Assert.Equal([1, 1, 1, 0], steps.Select(s => s.Reached));
    }

    [Fact]
    public void Funnel_FinalInterviewCountsAsPassingTechInterview()
    {
        var final = Create(Track.Developer, ApplicationStatus.FinalInterview);

        var steps = FunnelFor(Track.Developer, final);

        Assert.Equal([1, 1, 1, 0], steps.Select(s => s.Reached));
    }

    [Fact]
    public void Funnel_ExcludesApplicationsNotAppliedTo()
    {
        var interested = Create(Track.SDET, ApplicationStatus.Interested);

        var funnel = DashboardCalculator.Calculate([interested]).Funnels.Single(f => f.Track == Track.SDET);

        Assert.Equal(1, funnel.Total);
        Assert.Equal(0, funnel.Steps[0].Reached);
    }

    [Fact]
    public void Funnel_ConversionIsReachedOverPreviousStep()
    {
        var apps = new[]
        {
            Create(Track.Developer, ApplicationStatus.Applied),
            Create(Track.Developer, ApplicationStatus.Applied),
            Create(Track.Developer, ApplicationStatus.RecruiterScreen),
            Create(Track.Developer, ApplicationStatus.Offer),
        };

        var funnel = DashboardCalculator.Calculate(apps).Funnels.Single(f => f.Track == Track.Developer);

        Assert.Equal([4, 2, 1, 1], funnel.Steps.Select(s => s.Reached));
        Assert.Null(funnel.Steps[0].ConversionFromPrevious);
        Assert.Equal(0.5, funnel.Steps[1].ConversionFromPrevious);
        Assert.Equal(0.5, funnel.Steps[2].ConversionFromPrevious);
        Assert.Equal(1.0, funnel.Steps[3].ConversionFromPrevious);
        Assert.Equal(0.25, funnel.OverallConversion);
    }

    [Fact]
    public void Funnel_ConversionAfterAnEmptyStepIsNull()
    {
        var funnel = FunnelFor(Track.SDET, Create(Track.SDET, ApplicationStatus.Applied));

        Assert.Equal(0.0, funnel[1].ConversionFromPrevious); // 0 of 1 got a screen
        Assert.Null(funnel[2].ConversionFromPrevious); // nothing to convert from
    }

    [Fact]
    public void Funnel_IsSeparatePerTrack()
    {
        var stats = DashboardCalculator.Calculate(
        [
            Create(Track.Developer, ApplicationStatus.TechInterview),
            Create(Track.SDET, ApplicationStatus.Applied),
        ]);

        Assert.Equal(1, stats.Funnels.Single(f => f.Track == Track.Developer).Steps[2].Reached);
        Assert.Equal(0, stats.Funnels.Single(f => f.Track == Track.SDET).Steps[2].Reached);
    }

    private static IReadOnlyList<FunnelStep> FunnelFor(Track track, params Application[] apps) =>
        DashboardCalculator.Calculate(apps).Funnels.Single(f => f.Track == track).Steps;

    /// <summary>Creates an application and walks it through the given statuses, like a real history.</summary>
    private static Application Create(Track track, params ApplicationStatus[] history)
    {
        var application = new Application("Contoso", "Developer", track, Channel.LinkedIn, EmploymentType.Permanent);
        foreach (var status in history)
        {
            application.ChangeStatus(status, Today);
        }

        return application;
    }
}
