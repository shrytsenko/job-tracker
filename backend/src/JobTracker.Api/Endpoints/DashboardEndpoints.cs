using JobTracker.Core.Reporting;
using JobTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/", Get)
            .WithSummary("Counts per status and track, and a funnel per track (Applied → Recruiter screen → Tech interview → Offer)");

        return group;
    }

    private static async Task<Ok<DashboardStats>> Get(JobTrackerDbContext db, CancellationToken ct)
    {
        // A personal tracker holds hundreds of applications at most, so the stats are computed in memory
        // by the unit-tested calculator rather than in SQL.
        var applications = await db.Applications.AsNoTracking().ToListAsync(ct);
        return TypedResults.Ok(DashboardCalculator.Calculate(applications));
    }
}
