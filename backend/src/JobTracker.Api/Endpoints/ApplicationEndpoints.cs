using JobTracker.Api.Contracts;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Endpoints;

public static class ApplicationEndpoints
{
    public static RouteGroupBuilder MapApplicationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/applications").WithTags("Applications");

        // No body to validate. DisableValidation also works around a .NET 10 quirk where the validation
        // filter lets a failed query binding (e.g. ?status=NotAStatus) through as 200 instead of 400.
        group.MapGet("/", List)
            .DisableValidation()
            .WithSummary("List applications, optionally filtered by status, track, channel and a company/role search");
        group.MapGet("/follow-ups", FollowUps)
            .DisableValidation()
            .WithSummary("Open applications with no contact for N days (default 5) or a next action due today or earlier");
        group.MapGet("/cv-versions", CvVersions).WithSummary("Distinct CV version names used so far, for autocomplete");
        group.MapGet("/{id:guid}", GetById).WithName("GetApplication").WithSummary("Get one application with its contacts and timeline");
        group.MapPost("/", Create).WithSummary("Create an application");
        group.MapPut("/{id:guid}", Update).WithSummary("Update an application (full replace of editable fields)");
        group.MapDelete("/{id:guid}", Delete).WithSummary("Delete an application and its timeline");

        group.MapPost("/{id:guid}/activities", AddActivity).WithSummary("Add a timeline entry");

        group.MapPut("/{id:guid}/next-action", SetNextAction).WithSummary("Set or clear the next action (empty description clears it)");
        group.MapPost("/{id:guid}/next-action/complete", CompleteNextAction)
            .WithSummary("Mark the next action done: logs \"Done: …\" on the timeline and clears it");

        group.MapPut("/{id:guid}/contacts/{contactId:guid}", LinkContact).WithSummary("Link a contact to an application (idempotent)");
        group.MapDelete("/{id:guid}/contacts/{contactId:guid}", UnlinkContact).WithSummary("Unlink a contact from an application");

        return group;
    }

    private static async Task<Ok<List<ApplicationSummaryResponse>>> List(
        JobTrackerDbContext db,
        ApplicationStatus? status,
        Track? track,
        Channel? channel,
        string? search,
        CancellationToken ct)
    {
        var query = db.Applications.AsNoTracking();

        if (status is not null) query = query.Where(a => a.Status == status);
        if (track is not null) query = query.Where(a => a.Track == track);
        if (channel is not null) query = query.Where(a => a.Channel == channel);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILIKE: PostgreSQL's case-insensitive LIKE, so "contoso" finds "Contoso".
            var pattern = $"%{search.Trim()}%";
            query = query.Where(a => EF.Functions.ILike(a.Company, pattern) || EF.Functions.ILike(a.RoleTitle, pattern));
        }

        var applications = await query
            .OrderBy(a => a.Company)
            .ThenBy(a => a.RoleTitle)
            .ToListAsync(ct);

        return TypedResults.Ok(applications.Select(ApplicationSummaryResponse.From).ToList());
    }

    private static async Task<Results<Ok<List<FollowUpResponse>>, ValidationProblem>> FollowUps(
        JobTrackerDbContext db, TimeProvider clock, CancellationToken ct, int days = 5)
    {
        if (days is < 1 or > 365)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["days"] = ["Must be between 1 and 365."],
            });
        }

        var today = Today(clock);

        // The rule lives on the entity (unit-tested), so it's evaluated in memory. Closed applications are
        // filtered in SQL first; for a personal tracker the remaining set is small.
        var closed = new[] { ApplicationStatus.Accepted, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn, ApplicationStatus.NoResponse };
        var open = await db.Applications.AsNoTracking().Where(a => !closed.Contains(a.Status)).ToListAsync(ct);

        var followUps = open
            .Where(a => a.NeedsFollowUp(today, days))
            .OrderByDescending(a => a.IsNextActionDue(today))
            .ThenBy(a => a.NextActionDueOn)
            .ThenByDescending(a => a.DaysSinceContact(today))
            .Select(a => new FollowUpResponse(
                ApplicationSummaryResponse.From(a),
                a.DaysSinceContact(today),
                a.IsStale(today, days),
                a.IsNextActionDue(today)))
            .ToList();

        return TypedResults.Ok(followUps);
    }

    private static async Task<Ok<List<string>>> CvVersions(JobTrackerDbContext db, CancellationToken ct)
    {
        var versions = await db.Applications
            .Where(a => a.CvVersion != null)
            .Select(a => a.CvVersion!)
            .Distinct()
            .ToListAsync(ct);

        // Case-insensitive de-duplication, same as company suggestions.
        return TypedResults.Ok(versions
            .DistinctBy(v => v, StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound>> GetById(
        Guid id, JobTrackerDbContext db, CancellationToken ct)
    {
        var application = await db.Applications
            .AsNoTracking()
            .Include(a => a.Contacts)
            .Include(a => a.Activities)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return application is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static async Task<CreatedAtRoute<ApplicationDetailResponse>> Create(
        SaveApplicationRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var application = new Application(request.Company, request.RoleTitle, request.Track, request.Channel, request.EmploymentType);
        Apply(request, application, Today(clock));

        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);

        return TypedResults.CreatedAtRoute(
            ApplicationDetailResponse.From(application), "GetApplication", new { id = application.Id });
    }

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound>> Update(
        Guid id, SaveApplicationRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var application = await db.Applications
            .Include(a => a.Contacts)
            .Include(a => a.Activities)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (application is null) return TypedResults.NotFound();

        application.UpdateDetails(request.Company, request.RoleTitle, request.Track, request.Channel, request.EmploymentType);
        Apply(request, application, Today(clock));

        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static async Task<Results<NoContent, NotFound>> Delete(Guid id, JobTrackerDbContext db, CancellationToken ct)
    {
        var deleted = await db.Applications.Where(a => a.Id == id).ExecuteDeleteAsync(ct);
        return deleted == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    private static async Task<Results<Created<ActivityResponse>, NotFound>> AddActivity(
        Guid id, AddActivityRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var application = await db.Applications.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (application is null) return TypedResults.NotFound();

        var activity = application.AddActivity(request.Type, request.Text, request.Date ?? Today(clock));
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/applications/{id}", ActivityResponse.From(activity));
    }

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound>> SetNextAction(
        Guid id, SetNextActionRequest request, JobTrackerDbContext db, CancellationToken ct)
    {
        var application = await LoadWithDetailsAsync(db, id, ct);
        if (application is null) return TypedResults.NotFound();

        application.SetNextAction(request.Description, request.DueOn);
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound, ProblemHttpResult>> CompleteNextAction(
        Guid id, CompleteNextActionRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var application = await LoadWithDetailsAsync(db, id, ct);
        if (application is null) return TypedResults.NotFound();

        if (application.NextActionDescription is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Nothing to complete",
                detail: "This application has no next action.");
        }

        application.CompleteNextAction(request.Type, request.Date ?? Today(clock));
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static Task<Application?> LoadWithDetailsAsync(JobTrackerDbContext db, Guid id, CancellationToken ct) =>
        db.Applications
            .Include(a => a.Contacts)
            .Include(a => a.Activities)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    private static async Task<Results<NoContent, NotFound>> LinkContact(
        Guid id, Guid contactId, JobTrackerDbContext db, CancellationToken ct)
    {
        var application = await db.Applications.Include(a => a.Contacts).FirstOrDefaultAsync(a => a.Id == id, ct);
        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.Id == contactId, ct);
        if (application is null || contact is null) return TypedResults.NotFound();

        if (!application.Contacts.Contains(contact))
        {
            application.Contacts.Add(contact);
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> UnlinkContact(
        Guid id, Guid contactId, JobTrackerDbContext db, CancellationToken ct)
    {
        var application = await db.Applications.Include(a => a.Contacts).FirstOrDefaultAsync(a => a.Id == id, ct);
        var contact = application?.Contacts.FirstOrDefault(c => c.Id == contactId);
        if (application is null || contact is null) return TypedResults.NotFound();

        application.Contacts.Remove(contact);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    /// <summary>Copies the optional fields; status goes through the domain method so its rules apply.</summary>
    private static void Apply(SaveApplicationRequest request, Application application, DateOnly today)
    {
        application.JobUrl = request.JobUrl;
        application.CvVersion = string.IsNullOrWhiteSpace(request.CvVersion) ? null : request.CvVersion.Trim();
        application.RemoteRegion = request.RemoteRegion;
        application.HybridCity = request.HybridCity;
        application.CompensationAmount = request.CompensationAmount;
        application.CompensationCurrency = request.CompensationCurrency;
        application.CompensationPeriod = request.CompensationPeriod;
        application.CompensationIsNet = request.CompensationIsNet;
        application.AppliedOn = request.AppliedOn;
        application.LastContactOn = request.LastContactOn;
        application.SetNextAction(request.NextActionDescription, request.NextActionDueOn);
        application.Notes = request.Notes;

        if (request.Status is { } status && status != application.Status)
        {
            application.ChangeStatus(status, today);
        }
    }

    private static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
