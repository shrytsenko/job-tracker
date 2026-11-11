using JobTracker.Api.Contracts;
using JobTracker.Core.Ai;
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

        group.MapPut("/{id:guid}/status", UpdateStatus).WithSummary("Change only the status");
        group.MapPut("/{id:guid}/analysis", SaveAnalysis)
            .WithSummary("Save an analysis written elsewhere (e.g. Claude via MCP); validated like the built-in one");
        group.MapPost("/{id:guid}/analysis", Analyze)
            .WithSummary("Analyse the job post with AI against your CV summary (saves the post text, replaces the previous analysis)");

        group.MapPost("/{id:guid}/reply-draft", DraftReply)
            .WithSummary("Draft a reply to a recruiter with AI (returns editable text; nothing is sent)");

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

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound>> UpdateStatus(
        Guid id, UpdateStatusRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var application = await LoadWithDetailsAsync(db, id, ct);
        if (application is null) return TypedResults.NotFound();

        if (request.Status != application.Status)
        {
            application.ChangeStatus(request.Status, Today(clock));
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound, ValidationProblem>> SaveAnalysis(
        Guid id, SaveAnalysisRequest request, JobTrackerDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var application = await LoadWithDetailsAsync(db, id, ct);
        if (application is null) return TypedResults.NotFound();

        var jobDescription = string.IsNullOrWhiteSpace(request.JobDescription) ? application.JobDescription : request.JobDescription;
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["jobDescription"] = ["Include the job post that was analysed."],
            });
        }

        // JobAnalysis.Create validates (score 0-100, list sizes); a broken value becomes a 400 via the exception handler.
        var analysis = JobAnalysis.Create(
            request.Stack, request.Seniority, request.LocationRestrictions, request.EmploymentType, request.Salary,
            request.MatchScore, request.Strengths, request.Gaps, request.RedFlags,
            string.IsNullOrWhiteSpace(request.Model) ? "Claude (via MCP)" : request.Model, clock.GetUtcNow());

        application.SetJobDescription(jobDescription);
        application.RecordAnalysis(JobAnalyzer.ApplyRestrictionBackstop(analysis, application.JobDescription!));
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static async Task<Results<Ok<ApplicationDetailResponse>, NotFound, ValidationProblem, ProblemHttpResult>> Analyze(
        Guid id, AnalyzeJobRequest request, JobTrackerDbContext db, JobAnalyzer analyzer, TimeProvider clock,
        ILogger<JobAnalyzer> logger, CancellationToken ct)
    {
        var application = await LoadWithDetailsAsync(db, id, ct);
        if (application is null) return TypedResults.NotFound();

        var jobDescription = string.IsNullOrWhiteSpace(request.JobDescription) ? application.JobDescription : request.JobDescription;
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["jobDescription"] = ["Paste the job post to analyse."],
            });
        }

        var profile = await db.Profiles.FindAsync([Profile.SingletonId], ct);
        if (profile is not { HasCvSummary: true })
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "CV summary missing",
                detail: "Add your CV summary on the Profile page first, so the job post can be compared with it.");
        }

        // Save the pasted text first, so it is never lost if the AI call fails.
        application.SetJobDescription(jobDescription);
        await db.SaveChangesAsync(ct);

        try
        {
            var analysis = await analyzer.AnalyzeAsync(application.JobDescription!, profile.CvSummary, clock.GetUtcNow(), ct);
            application.RecordAnalysis(analysis);
            await db.SaveChangesAsync(ct);
        }
        catch (LlmException ex)
        {
            // The job text and CV are deliberately not logged (personal data); the failure kind is enough to diagnose.
            logger.LogWarning("Job analysis for application {ApplicationId} failed: {Failure}", id, ex.Failure);
            return TypedResults.Problem(statusCode: StatusFor(ex.Failure), title: "Analysis failed", detail: ex.Message);
        }

        return TypedResults.Ok(ApplicationDetailResponse.From(application));
    }

    private static async Task<Results<Ok<ReplyDraftResponse>, NotFound, ValidationProblem, ProblemHttpResult>> DraftReply(
        Guid id, DraftReplyRequest request, JobTrackerDbContext db, ReplyDrafter drafter, ILogger<ReplyDrafter> logger,
        CancellationToken ct)
    {
        var application = await LoadWithDetailsAsync(db, id, ct);
        if (application is null) return TypedResults.NotFound();

        var recipient = request.ContactId is { } contactId ? application.Contacts.FirstOrDefault(c => c.Id == contactId) : null;
        if (request.ContactId is not null && recipient is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["contactId"] = ["Choose one of the contacts linked to this application."],
            });
        }

        // The CV summary and name are optional for drafts: a reply can be written from the application alone.
        var profile = await db.Profiles.FindAsync([Profile.SingletonId], ct);

        var context = new ReplyContext(
            application.Company,
            application.RoleTitle,
            application.Status,
            application.NextActionDescription,
            application.JobDescription,
            application.Analysis,
            profile?.CvSummary,
            profile?.FullName,
            recipient?.Name,
            recipient?.Role,
            request.TheirMessage,
            request.Intent);

        try
        {
            var draft = await drafter.DraftAsync(context, request.Channel, request.Tone, ct);
            return TypedResults.Ok(new ReplyDraftResponse(draft.Subject, draft.Body, draft.Model));
        }
        catch (LlmException ex)
        {
            logger.LogWarning("Reply draft for application {ApplicationId} failed: {Failure}", id, ex.Failure);
            return TypedResults.Problem(statusCode: StatusFor(ex.Failure), title: "Draft failed", detail: ex.Message);
        }
    }

    private static int StatusFor(LlmFailure failure) => failure switch
    {
        LlmFailure.NotConfigured => StatusCodes.Status503ServiceUnavailable,
        LlmFailure.NoCredits => StatusCodes.Status402PaymentRequired,
        LlmFailure.Timeout => StatusCodes.Status504GatewayTimeout,
        LlmFailure.RateLimited => StatusCodes.Status429TooManyRequests,
        LlmFailure.Refused => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status502BadGateway, // InvalidOutput, ProviderError: the upstream service gave us nothing usable
    };

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
