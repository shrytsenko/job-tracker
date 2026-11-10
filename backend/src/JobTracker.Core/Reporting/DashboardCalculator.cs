using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Reporting;

public sealed record StatusCount(ApplicationStatus Status, int Count);

public sealed record TrackCount(Track Track, int Count);

/// <param name="Reached">Applications whose furthest stage is this stage or later.</param>
/// <param name="ConversionFromPrevious">Reached / reached at the previous step (0–1); null for the first step or when the previous step is 0.</param>
public sealed record FunnelStep(ApplicationStatus Stage, int Reached, double? ConversionFromPrevious);

/// <param name="Total">All applications on the track, including ones not applied to yet.</param>
/// <param name="OverallConversion">Offers / applied (0–1); null when nothing was applied to.</param>
public sealed record TrackFunnel(Track Track, int Total, IReadOnlyList<FunnelStep> Steps, double? OverallConversion);

public sealed record DashboardStats(
    int Total,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<TrackCount> ByTrack,
    IReadOnlyList<TrackFunnel> Funnels);

/// <summary>
/// Turns applications into dashboard numbers. Pure: no database, no clock, so the maths is unit-tested directly.
/// </summary>
public static class DashboardCalculator
{
    /// <summary>
    /// The funnel steps from the project brief. Final interview is not a step of its own,
    /// but reaching it still counts as having passed the tech interview.
    /// </summary>
    public static readonly IReadOnlyList<ApplicationStatus> FunnelStages =
    [
        ApplicationStatus.Applied,
        ApplicationStatus.RecruiterScreen,
        ApplicationStatus.TechInterview,
        ApplicationStatus.Offer,
    ];

    public static DashboardStats Calculate(IReadOnlyCollection<Application> applications)
    {
        // Every status and track is listed, zeros included, so the UI never has to fill gaps.
        var byStatus = Enum.GetValues<ApplicationStatus>()
            .Select(status => new StatusCount(status, applications.Count(a => a.Status == status)))
            .ToList();

        var byTrack = Enum.GetValues<Track>()
            .Select(track => new TrackCount(track, applications.Count(a => a.Track == track)))
            .ToList();

        var funnels = Enum.GetValues<Track>()
            .Select(track => BuildFunnel(track, applications.Where(a => a.Track == track).ToList()))
            .ToList();

        return new DashboardStats(applications.Count, byStatus, byTrack, funnels);
    }

    private static TrackFunnel BuildFunnel(Track track, IReadOnlyCollection<Application> applications)
    {
        var steps = new List<FunnelStep>();
        int? previous = null;

        foreach (var stage in FunnelStages)
        {
            var reached = applications.Count(a => HasReached(a, stage));
            double? conversion = previous is > 0 ? (double)reached / previous.Value : null;
            steps.Add(new FunnelStep(stage, reached, conversion));
            previous = reached;
        }

        var applied = steps[0].Reached;
        var offers = steps[^1].Reached;
        return new TrackFunnel(track, applications.Count, steps, applied > 0 ? (double)offers / applied : null);
    }

    /// <summary>
    /// Uses the furthest stage, not the current status: an application rejected after the tech interview
    /// still counts as having reached Applied, Recruiter screen and Tech interview.
    /// </summary>
    private static bool HasReached(Application application, ApplicationStatus stage) =>
        application.FurthestStage is { } furthest && Rank(furthest) >= Rank(stage);

    private static readonly Dictionary<ApplicationStatus, int> StageRanks =
        Application.PipelineStages.Select((stage, index) => (stage, index)).ToDictionary(x => x.stage, x => x.index);

    private static int Rank(ApplicationStatus stage) =>
        StageRanks.TryGetValue(stage, out var rank)
            ? rank
            : throw new ArgumentOutOfRangeException(nameof(stage), stage, "Not a pipeline stage.");
}
