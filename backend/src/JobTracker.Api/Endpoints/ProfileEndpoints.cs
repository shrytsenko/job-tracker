using JobTracker.Api.Contracts;
using JobTracker.Core.Entities;
using JobTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JobTracker.Api.Endpoints;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/profile").WithTags("Profile");

        group.MapGet("/", Get).WithSummary("Your profile: the CV summary job posts are compared against");
        group.MapPut("/", Save).WithSummary("Save your CV summary");

        return group;
    }

    private static async Task<Ok<ProfileResponse>> Get(JobTrackerDbContext db, CancellationToken ct)
    {
        var profile = await db.Profiles.FindAsync([Profile.SingletonId], ct);
        return TypedResults.Ok(profile is null
            ? new ProfileResponse(string.Empty, null)
            : new ProfileResponse(profile.CvSummary, profile.UpdatedAt));
    }

    private static async Task<Ok<ProfileResponse>> Save(
        SaveProfileRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var profile = await db.Profiles.FindAsync([Profile.SingletonId], ct);
        if (profile is null)
        {
            profile = new Profile();
            db.Profiles.Add(profile);
        }

        profile.UpdateCvSummary(request.CvSummary, clock.GetUtcNow());
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new ProfileResponse(profile.CvSummary, profile.UpdatedAt));
    }
}
