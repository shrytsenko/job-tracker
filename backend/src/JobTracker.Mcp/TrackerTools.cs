using System.ComponentModel;
using System.Text.Json;
using JobTracker.Core.Ai;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace JobTracker.Mcp;

/// <summary>
/// The tools Claude can use. Each one is a thin call to the tracker's REST API, so the same validation and
/// domain rules apply as in the web UI. There is deliberately no delete tool: everything Claude can do is
/// reversible from the UI.
/// </summary>
[McpServerToolType]
public static class TrackerTools
{
    private const string Statuses =
        "Interested, Applied, RecruiterScreen, TechInterview, FinalInterview, Offer, Accepted, Rejected, Withdrawn, NoResponse";

    [McpServerTool(Name = "list_applications", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("List job applications (id, company, role, status, track, channel, dates, next action). All filters are optional.")]
    public static Task<string> ListApplications(
        TrackerApi api,
        [Description("Filter by status: " + Statuses)] string? status = null,
        [Description("Filter by track: Developer, SDET, Integration, Other")] string? track = null,
        [Description("Filter by channel: LinkedIn, Djinni, Email, Referral, Platform, CompanyWebsite, Other")] string? channel = null,
        [Description("Case-insensitive search in company and role")] string? search = null,
        CancellationToken ct = default) =>
        api.GetAsync("/api/applications" + Query(("status", status), ("track", track), ("channel", channel), ("search", search)), ct);

    [McpServerTool(Name = "get_application", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Full details of one application: contacts, timeline, next action, job post and AI analysis.")]
    public static Task<string> GetApplication(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        CancellationToken ct = default) =>
        api.GetAsync($"/api/applications/{Id(applicationId)}", ct);

    [McpServerTool(Name = "add_application", Destructive = false, OpenWorld = false)]
    [Description("Add a new job application. Returns the created application including its id.")]
    public static Task<string> AddApplication(
        TrackerApi api,
        [Description("Company name")] string company,
        [Description("Role title as written in the post")] string roleTitle,
        [Description("Developer, SDET, Integration or Other")] string track = "Developer",
        [Description("LinkedIn, Djinni, Email, Referral, Platform, CompanyWebsite or Other")] string channel = "LinkedIn",
        [Description("Permanent, Contract or Freelance")] string employmentType = "Permanent",
        [Description("Starting status, usually Interested or Applied")] string status = "Interested",
        [Description("Link to the job post")] string? jobUrl = null,
        [Description("Remote region as stated, e.g. Remote EU or US only")] string? remoteRegion = null,
        [Description("Free-text notes")] string? notes = null,
        CancellationToken ct = default) =>
        api.PostAsync("/api/applications", new { company, roleTitle, track, channel, employmentType, status, jobUrl, remoteRegion, notes }, ct);

    [McpServerTool(Name = "update_status", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Change an application's status. Moving to Applied records the applied date; closing statuses keep the furthest stage reached.")]
    public static Task<string> UpdateStatus(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        [Description("New status: " + Statuses)] string status,
        CancellationToken ct = default) =>
        api.PutAsync($"/api/applications/{Id(applicationId)}/status", new { status }, ct);

    [McpServerTool(Name = "log_activity", Destructive = false, OpenWorld = false)]
    [Description("Add an entry to an application's timeline. MessageSent, ReplyReceived, Interview and TakeHome count as contact and update the last contact date; Note does not.")]
    public static Task<string> LogActivity(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        [Description("MessageSent, ReplyReceived, Interview, TakeHome or Note")] string type,
        [Description("Short description of what happened")] string text,
        [Description("Date as YYYY-MM-DD; defaults to today")] string? date = null,
        CancellationToken ct = default) =>
        api.PostAsync($"/api/applications/{Id(applicationId)}/activities", new { type, text, date }, ct);

    [McpServerTool(Name = "get_followups", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Open applications that need attention: no contact for N days, or a next action due today or earlier, with the reason for each.")]
    public static Task<string> GetFollowUps(
        TrackerApi api,
        [Description("Days without contact before an in-progress application counts as stale (1-365)")] int days = 5,
        CancellationToken ct = default) =>
        api.GetAsync($"/api/applications/follow-ups?days={days}", ct);

    [McpServerTool(Name = "set_next_action", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Set what to do next on an application and optionally by when; it shows in follow-ups when due.")]
    public static Task<string> SetNextAction(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        [Description("The next step, e.g. Follow up with Dana")] string description,
        [Description("Due date as YYYY-MM-DD (optional)")] string? dueOn = null,
        CancellationToken ct = default) =>
        api.PutAsync($"/api/applications/{Id(applicationId)}/next-action", new { description, dueOn }, ct);

    [McpServerTool(Name = "complete_next_action", Destructive = false, OpenWorld = false)]
    [Description("Mark the next action as done: logs \"Done: ...\" on the timeline and clears it.")]
    public static Task<string> CompleteNextAction(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        [Description("How to log it: Note (private task) or MessageSent, ReplyReceived, Interview, TakeHome (counts as contact)")] string type = "Note",
        [Description("Date as YYYY-MM-DD; defaults to today")] string? date = null,
        CancellationToken ct = default) =>
        api.PostAsync($"/api/applications/{Id(applicationId)}/next-action/complete", new { type, date }, ct);

    [McpServerTool(Name = "get_job_post", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Everything needed to analyse an application's job post: the post text, the candidate's CV summary, and the analysis rules. Analyse it following the rules, then call save_job_analysis.")]
    public static async Task<string> GetJobPost(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        CancellationToken ct = default)
    {
        var application = TrackerApi.Parse(await api.GetAsync($"/api/applications/{Id(applicationId)}", ct));
        var profile = TrackerApi.Parse(await api.GetAsync("/api/profile", ct));

        var jobDescription = application.TryGetProperty("jobDescription", out var jd) ? jd.GetString() : null;
        if (string.IsNullOrWhiteSpace(jobDescription))
        {
            throw new McpException("This application has no job post saved yet. Ask the user to paste it, then pass it as jobDescription to save_job_analysis.");
        }

        return JsonSerializer.Serialize(new
        {
            company = application.GetProperty("company").GetString(),
            roleTitle = application.GetProperty("roleTitle").GetString(),
            jobDescription,
            candidateCvSummary = profile.GetProperty("cvSummary").GetString(),
            // The same rules the built-in analysis uses, so both produce comparable results.
            analysisRules = JobAnalyzer.SystemPrompt,
        });
    }

    [McpServerTool(Name = "save_job_analysis", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Save your analysis of an application's job post (replaces the previous one). It is validated like the built-in analysis, and location or work-permit restrictions in the post are always added as red flags.")]
    public static Task<string> SaveJobAnalysis(
        TrackerApi api,
        [Description("Application id from list_applications")] string applicationId,
        [Description("0-100: how well the CV fits the role's requirements")] int matchScore,
        [Description("Technologies the post asks for, as short names")] string[] stack,
        [Description("Concrete overlaps between the CV and the post")] string[] strengths,
        [Description("Requirements the CV does not show")] string[] gaps,
        [Description("Only things likely to stop this candidate; empty if none")] string[] redFlags,
        [Description("Seniority as stated, or null")] string? seniority = null,
        [Description("Location restrictions as stated, e.g. Remote EU, or null")] string? locationRestrictions = null,
        [Description("Employment type as stated, or null")] string? employmentType = null,
        [Description("Salary as stated, with currency and period, or null")] string? salary = null,
        [Description("The job post text, if it isn't saved on the application yet")] string? jobDescription = null,
        CancellationToken ct = default) =>
        api.PutAsync($"/api/applications/{Id(applicationId)}/analysis", new
        {
            matchScore, stack, strengths, gaps, redFlags, seniority, locationRestrictions, employmentType, salary, jobDescription,
            model = "Claude (via MCP)",
        }, ct);

    /// <summary>Rejects anything that isn't an id before it becomes part of a URL.</summary>
    private static Guid Id(string applicationId) =>
        Guid.TryParse(applicationId, out var id)
            ? id
            : throw new McpException($"\"{applicationId}\" is not an application id. Use list_applications to find the id.");

    private static string Query(params (string Key, string? Value)[] parameters)
    {
        var pairs = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!.Trim())}")
            .ToList();
        return pairs.Count == 0 ? string.Empty : "?" + string.Join("&", pairs);
    }
}
