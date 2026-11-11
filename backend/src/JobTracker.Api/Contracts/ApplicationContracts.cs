using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Ai;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Api.Contracts;

/// <summary>Body for creating (POST) or fully updating (PUT) an application.</summary>
/// <param name="Status">Optional. On create defaults to Interested; on update, null keeps the current status.</param>
public record SaveApplicationRequest(
    [Required, MaxLength(200)] string Company,
    [Required, MaxLength(200)] string RoleTitle,
    Track Track,
    Channel Channel,
    EmploymentType EmploymentType,
    ApplicationStatus? Status = null,
    [Url, MaxLength(2000)] string? JobUrl = null,
    [MaxLength(100)] string? CvVersion = null,
    [MaxLength(100)] string? RemoteRegion = null,
    [MaxLength(100)] string? HybridCity = null,
    [Range(0, 100_000_000)] decimal? CompensationAmount = null,
    Currency? CompensationCurrency = null,
    CompensationPeriod? CompensationPeriod = null,
    bool? CompensationIsNet = null,
    DateOnly? AppliedOn = null,
    DateOnly? LastContactOn = null,
    [MaxLength(500)] string? NextActionDescription = null,
    DateOnly? NextActionDueOn = null,
    string? Notes = null);

public record AddActivityRequest(
    ActivityType Type,
    [Required, MaxLength(2000)] string Text,
    DateOnly? Date = null);

/// <summary>Sets the next action. An empty description clears it; a due date needs a description.</summary>
public record SetNextActionRequest(
    [MaxLength(500)] string? Description,
    DateOnly? DueOn = null);

/// <summary>Marks the next action done, logging it on the timeline as <paramref name="Type"/>.</summary>
/// <param name="Type">Note for private tasks; MessageSent / ReplyReceived / Interview also count as contact.</param>
/// <param name="Date">Defaults to today.</param>
public record CompleteNextActionRequest(
    ActivityType Type = ActivityType.Note,
    DateOnly? Date = null);

public record ApplicationSummaryResponse(
    Guid Id,
    string Company,
    string RoleTitle,
    Track Track,
    Channel Channel,
    ApplicationStatus Status,
    ApplicationStatus? FurthestStage,
    EmploymentType EmploymentType,
    DateOnly? AppliedOn,
    DateOnly? LastContactOn,
    string? NextActionDescription,
    DateOnly? NextActionDueOn)
{
    public static ApplicationSummaryResponse From(Application a) => new(
        a.Id, a.Company, a.RoleTitle, a.Track, a.Channel, a.Status, a.FurthestStage, a.EmploymentType,
        a.AppliedOn, a.LastContactOn, a.NextActionDescription, a.NextActionDueOn);
}

public record ApplicationDetailResponse(
    Guid Id,
    string Company,
    string RoleTitle,
    string? JobUrl,
    string? CvVersion,
    Track Track,
    Channel Channel,
    ApplicationStatus Status,
    ApplicationStatus? FurthestStage,
    EmploymentType EmploymentType,
    string? RemoteRegion,
    string? HybridCity,
    decimal? CompensationAmount,
    Currency? CompensationCurrency,
    CompensationPeriod? CompensationPeriod,
    bool? CompensationIsNet,
    DateOnly? AppliedOn,
    DateOnly? LastContactOn,
    string? NextActionDescription,
    DateOnly? NextActionDueOn,
    string? Notes,
    string? JobDescription,
    JobAnalysisResponse? Analysis,
    IReadOnlyList<ContactSummaryResponse> Contacts,
    IReadOnlyList<ActivityResponse> Activities)
{
    public static ApplicationDetailResponse From(Application a) => new(
        a.Id, a.Company, a.RoleTitle, a.JobUrl, a.CvVersion, a.Track, a.Channel, a.Status, a.FurthestStage, a.EmploymentType,
        a.RemoteRegion, a.HybridCity,
        a.CompensationAmount, a.CompensationCurrency, a.CompensationPeriod, a.CompensationIsNet,
        a.AppliedOn, a.LastContactOn, a.NextActionDescription, a.NextActionDueOn, a.Notes,
        a.JobDescription, a.Analysis is null ? null : JobAnalysisResponse.From(a.Analysis),
        a.Contacts.OrderBy(c => c.Name).Select(ContactSummaryResponse.From).ToList(),
        a.Activities.OrderByDescending(x => x.Date).Select(ActivityResponse.From).ToList());
}

/// <param name="DaysSinceContact">Days since last contact (or applied date); null if neither is known.</param>
/// <param name="IsStale">In progress with no contact for at least the requested number of days.</param>
/// <param name="IsNextActionDue">Next action is due today or overdue.</param>
public record FollowUpResponse(
    ApplicationSummaryResponse Application,
    int? DaysSinceContact,
    bool IsStale,
    bool IsNextActionDue);

public record ActivityResponse(Guid Id, ActivityType Type, string Text, DateOnly Date)
{
    public static ActivityResponse From(Activity x) => new(x.Id, x.Type, x.Text, x.Date);
}

/// <summary>Body for drafting a reply. Every field is optional; the more context, the better the draft.</summary>
/// <param name="ContactId">One of the contacts linked to the application, used as the recipient.</param>
/// <param name="TheirMessage">The recruiter's message to reply to, pasted in.</param>
/// <param name="Intent">What you want to say, e.g. "interested, ask about the salary range".</param>
public record DraftReplyRequest(
    ReplyChannel Channel = ReplyChannel.LinkedIn,
    ReplyTone Tone = ReplyTone.Short,
    Guid? ContactId = null,
    [MaxLength(10000)] string? TheirMessage = null,
    [MaxLength(2000)] string? Intent = null);

/// <param name="Subject">Email subject; null for LinkedIn.</param>
public record ReplyDraftResponse(string? Subject, string Body, string Model);

/// <summary>Body for changing only the status (the pipeline rules, e.g. furthest stage, still apply).</summary>
public record UpdateStatusRequest(ApplicationStatus Status);

/// <summary>
/// An analysis written elsewhere (e.g. by Claude through the MCP server). It goes through the same validation
/// and the same restriction backstop as the built-in analysis.
/// </summary>
/// <param name="JobDescription">The post that was analysed. Omit to keep the text already saved on the application.</param>
/// <param name="Model">Who produced it, e.g. "Claude (via MCP)".</param>
public record SaveAnalysisRequest(
    int MatchScore,
    [MaxLength(Application.MaxJobDescriptionLength)] string? JobDescription = null,
    List<string>? Stack = null,
    string? Seniority = null,
    string? LocationRestrictions = null,
    string? EmploymentType = null,
    string? Salary = null,
    List<string>? Strengths = null,
    List<string>? Gaps = null,
    List<string>? RedFlags = null,
    [MaxLength(100)] string? Model = null);

/// <summary>Body for analysing an application's job post.</summary>
/// <param name="JobDescription">The pasted job post. Omit (null) to re-analyse the text already saved on the application.</param>
public record AnalyzeJobRequest([MaxLength(Application.MaxJobDescriptionLength)] string? JobDescription = null);

public record JobAnalysisResponse(
    IReadOnlyList<string> Stack,
    string? Seniority,
    string? LocationRestrictions,
    string? EmploymentType,
    string? Salary,
    int MatchScore,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Gaps,
    IReadOnlyList<string> RedFlags,
    string Model,
    DateTimeOffset AnalyzedAt)
{
    public static JobAnalysisResponse From(JobAnalysis a) => new(
        a.Stack, a.Seniority, a.LocationRestrictions, a.EmploymentType, a.Salary, a.MatchScore,
        a.Strengths, a.Gaps, a.RedFlags, a.Model, a.AnalyzedAt);
}
