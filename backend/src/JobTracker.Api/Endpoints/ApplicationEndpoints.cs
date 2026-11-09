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
            .WithSummary("List applications, optionally filtered by status, track and channel");
        group.MapGet("/{id:guid}", GetById).WithName("GetApplication").WithSummary("Get one application with its contacts and timeline");
        group.MapPost("/", Create).WithSummary("Create an application");
        group.MapPut("/{id:guid}", Update).WithSummary("Update an application (full replace of editable fields)");
        group.MapDelete("/{id:guid}", Delete).WithSummary("Delete an application and its timeline");

        group.MapPost("/{id:guid}/activities", AddActivity).WithSummary("Add a timeline entry");

        group.MapPut("/{id:guid}/contacts/{contactId:guid}", LinkContact).WithSummary("Link a contact to an application (idempotent)");
        group.MapDelete("/{id:guid}/contacts/{contactId:guid}", UnlinkContact).WithSummary("Unlink a contact from an application");

        return group;
    }

    private static async Task<Ok<List<ApplicationSummaryResponse>>> List(
        JobTrackerDbContext db,
        ApplicationStatus? status,
        Track? track,
        Channel? channel,
        CancellationToken ct)
    {
        var query = db.Applications.AsNoTracking();

        if (status is not null) query = query.Where(a => a.Status == status);
        if (track is not null) query = query.Where(a => a.Track == track);
        if (channel is not null) query = query.Where(a => a.Channel == channel);

        var applications = await query
            .OrderBy(a => a.Company)
            .ThenBy(a => a.RoleTitle)
            .ToListAsync(ct);

        return TypedResults.Ok(applications.Select(ApplicationSummaryResponse.From).ToList());
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
        application.RemoteRegion = request.RemoteRegion;
        application.HybridCity = request.HybridCity;
        application.CompensationAmount = request.CompensationAmount;
        application.CompensationCurrency = request.CompensationCurrency?.ToUpperInvariant();
        application.CompensationPeriod = request.CompensationPeriod;
        application.CompensationIsNet = request.CompensationIsNet;
        application.AppliedOn = request.AppliedOn;
        application.LastContactOn = request.LastContactOn;
        application.NextActionDescription = request.NextActionDescription;
        application.NextActionDueOn = request.NextActionDueOn;
        application.Notes = request.Notes;

        if (request.Status is { } status && status != application.Status)
        {
            application.ChangeStatus(status, today);
        }
    }

    private static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
}
