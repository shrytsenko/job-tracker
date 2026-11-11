using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>
/// One job the candidate is interested in or has applied to: its pipeline status, timeline, contacts, next action
/// and optional AI analysis. The rules that keep these consistent live here as methods.
/// </summary>
public class Application
{
    /// <summary>Unique id, generated on creation.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>The hiring company. Required; change it through <see cref="UpdateDetails"/>.</summary>
    public string Company { get; private set; } = null!;

    /// <summary>The job title as posted. Required; change it through <see cref="UpdateDetails"/>.</summary>
    public string RoleTitle { get; private set; } = null!;

    /// <summary>Link to the job post.</summary>
    public string? JobUrl { get; set; }

    /// <summary>Which CV version was sent, e.g. "Developer CV v3". Free text with suggestions in the UI.</summary>
    public string? CvVersion { get; set; }

    /// <summary>The career track it belongs to. Change it through <see cref="UpdateDetails"/>.</summary>
    public Track Track { get; private set; }

    /// <summary>Where it came from. Change it through <see cref="UpdateDetails"/>.</summary>
    public Channel Channel { get; private set; }

    /// <summary>Permanent, contract or freelance. Change it through <see cref="UpdateDetails"/>.</summary>
    public EmploymentType EmploymentType { get; private set; }

    /// <summary>Where it stands now. Starts as Interested; change it through <see cref="ChangeStatus"/>.</summary>
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Interested;

    /// <summary>
    /// The furthest pipeline stage (Applied → Offer) this application reached. Kept when it closes,
    /// so "rejected after the tech interview" is known — needed for the pipeline view and funnel stats.
    /// </summary>
    public ApplicationStatus? FurthestStage { get; private set; }

    /// <summary>Where remote work is allowed from, e.g. "Remote EU" or "US only".</summary>
    public string? RemoteRegion { get; set; }

    /// <summary>The office city, for hybrid roles.</summary>
    public string? HybridCity { get; set; }

    /// <summary>The offered or expected pay, in <see cref="CompensationCurrency"/> per <see cref="CompensationPeriod"/>.</summary>
    public decimal? CompensationAmount { get; set; }

    /// <summary>The currency of <see cref="CompensationAmount"/>.</summary>
    public Currency? CompensationCurrency { get; set; }

    /// <summary>The time unit of <see cref="CompensationAmount"/>.</summary>
    public CompensationPeriod? CompensationPeriod { get; set; }

    /// <summary>True for net (after tax), false for gross; null when unknown.</summary>
    public bool? CompensationIsNet { get; set; }

    /// <summary>When the application was sent. Filled in automatically on the first move to Applied.</summary>
    public DateOnly? AppliedOn { get; set; }

    /// <summary>
    /// The last real interaction with the company. Moved forward by <see cref="AddActivity"/>; drives the
    /// follow-up rule.
    /// </summary>
    public DateOnly? LastContactOn { get; set; }

    /// <summary>What to do next, e.g. "Send the test task". Set through <see cref="SetNextAction"/>.</summary>
    public string? NextActionDescription { get; private set; }

    /// <summary>When the next action is due; only set together with a description.</summary>
    public DateOnly? NextActionDueOn { get; private set; }

    /// <summary>Longest allowed <see cref="Notes"/>, enforced by the API and the database.</summary>
    public const int MaxNotesLength = 10000;

    /// <summary>Free-form notes (markdown).</summary>
    public string? Notes { get; set; }

    /// <summary>Longest allowed <see cref="JobDescription"/>, which keeps the AI prompt (and its cost) bounded.</summary>
    public const int MaxJobDescriptionLength = 30000;

    /// <summary>The pasted job post text, kept so the analysis can be re-run.</summary>
    public string? JobDescription { get; private set; }

    /// <summary>The latest AI analysis of <see cref="JobDescription"/>; null until one succeeds.</summary>
    public JobAnalysis? Analysis { get; private set; }

    /// <summary>The people linked to this application (many-to-many).</summary>
    public List<Contact> Contacts { get; private set; } = new();

    /// <summary>The timeline. Add entries through <see cref="AddActivity"/>.</summary>
    public List<Activity> Activities { get; private set; } = new();

    private Application()
    {
    }

    /// <summary>Creates an application with the required fields, in the Interested status.</summary>
    /// <exception cref="ArgumentException"><paramref name="company"/> or <paramref name="roleTitle"/> is empty.</exception>
    public Application(string company, string roleTitle, Track track, Channel channel, EmploymentType employmentType)
    {
        UpdateDetails(company, roleTitle, track, channel, employmentType);
    }

    /// <summary>Sets the required identifying fields, enforcing the same rules as the constructor.</summary>
    public void UpdateDetails(string company, string roleTitle, Track track, Channel channel, EmploymentType employmentType)
    {
        Company = Guard.AgainstNullOrWhiteSpace(company, nameof(company));
        RoleTitle = Guard.AgainstNullOrWhiteSpace(roleTitle, nameof(roleTitle));
        Track = track;
        Channel = channel;
        EmploymentType = employmentType;
    }

    /// <summary>Changes the status. Moving to Applied records today as the applied date if none was set.</summary>
    public void ChangeStatus(ApplicationStatus newStatus, DateOnly today)
    {
        Status = newStatus;

        // Being *at* a pipeline stage means that is the furthest reached; moving back is a correction.
        // Closing statuses keep the stage where the application ended.
        FurthestStage = newStatus switch
        {
            ApplicationStatus.Interested => null,
            ApplicationStatus.Accepted => ApplicationStatus.Offer,
            _ when PipelineStages.Contains(newStatus) => newStatus,
            _ => FurthestStage,
        };

        if (newStatus == ApplicationStatus.Applied && AppliedOn is null)
        {
            AppliedOn = today;
        }
    }

    /// <summary>
    /// Appends a timeline entry. Any real interaction (everything except a private note)
    /// moves LastContactOn forward, which drives the "needs follow-up" view.
    /// </summary>
    public Activity AddActivity(ActivityType type, string text, DateOnly date)
    {
        var activity = new Activity(Id, type, text, date);
        Activities.Add(activity);

        if (type != ActivityType.Note && (LastContactOn is null || date > LastContactOn))
        {
            LastContactOn = date;
        }

        return activity;
    }

    /// <summary>
    /// Sets what to do next and (optionally) by when. An empty description clears the next action;
    /// a due date on its own is rejected, because "due Oct 26" with nothing to do is meaningless.
    /// </summary>
    public void SetNextAction(string? description, DateOnly? dueOn)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            if (dueOn is not null)
            {
                throw new ArgumentException("A next action due date needs a description.", nameof(description));
            }

            NextActionDescription = null;
            NextActionDueOn = null;
            return;
        }

        NextActionDescription = description.Trim();
        NextActionDueOn = dueOn;
    }

    /// <summary>
    /// Marks the next action as done: records "Done: …" on the timeline as the given activity type
    /// (so a sent message or a reply also counts as contact) and clears the next action.
    /// </summary>
    public Activity CompleteNextAction(ActivityType type, DateOnly date)
    {
        if (NextActionDescription is null)
        {
            throw new InvalidOperationException("There is no next action to complete.");
        }

        var activity = AddActivity(type, $"Done: {NextActionDescription}", date);
        SetNextAction(null, null);
        return activity;
    }

    /// <summary>
    /// Stores the job post text. A different text makes the old analysis stale, so it is cleared;
    /// the same text (re-analysing) keeps it until the new analysis arrives.
    /// </summary>
    public void SetJobDescription(string? jobDescription)
    {
        var trimmed = string.IsNullOrWhiteSpace(jobDescription) ? null : jobDescription.Trim();
        if (trimmed?.Length > MaxJobDescriptionLength)
        {
            throw new ArgumentException($"Job description can be at most {MaxJobDescriptionLength} characters.", nameof(jobDescription));
        }

        if (trimmed != JobDescription)
        {
            Analysis = null;
        }

        JobDescription = trimmed;
    }

    /// <summary>Replaces the previous analysis (only the latest one is kept).</summary>
    public void RecordAnalysis(JobAnalysis analysis)
    {
        if (JobDescription is null)
        {
            throw new InvalidOperationException("Set a job description before recording its analysis.");
        }

        Analysis = analysis;
    }

    /// <summary>The hiring pipeline in order. Interested comes before it; closing statuses end it.</summary>
    public static readonly IReadOnlyList<ApplicationStatus> PipelineStages =
    [
        ApplicationStatus.Applied,
        ApplicationStatus.RecruiterScreen,
        ApplicationStatus.TechInterview,
        ApplicationStatus.FinalInterview,
        ApplicationStatus.Offer,
    ];

    /// <summary>The search is over for this application: it never needs a follow-up.</summary>
    public bool IsClosed => Status is ApplicationStatus.Accepted
        or ApplicationStatus.Rejected
        or ApplicationStatus.Withdrawn
        or ApplicationStatus.NoResponse;

    /// <summary>Statuses where the ball is in play with the company, so silence means "chase it".</summary>
    public bool IsInProgress => Status is ApplicationStatus.Applied
        or ApplicationStatus.RecruiterScreen
        or ApplicationStatus.TechInterview
        or ApplicationStatus.FinalInterview
        or ApplicationStatus.Offer;

    /// <summary>Days since the last contact, falling back to the applied date; null if neither is known.</summary>
    public int? DaysSinceContact(DateOnly today) =>
        (LastContactOn ?? AppliedOn) is { } since ? today.DayNumber - since.DayNumber : null;

    /// <summary>In progress and no contact for at least <paramref name="staleAfterDays"/> days.</summary>
    public bool IsStale(DateOnly today, int staleAfterDays) =>
        IsInProgress && DaysSinceContact(today) >= staleAfterDays;

    /// <summary>A next action is due today or already overdue, on an application that is still open.</summary>
    public bool IsNextActionDue(DateOnly today) =>
        !IsClosed && NextActionDueOn is { } due && due <= today;

    /// <summary>
    /// The "needs follow-up" rule: no contact for <paramref name="staleAfterDays"/> days while in progress, or a
    /// next action that is due.
    /// </summary>
    public bool NeedsFollowUp(DateOnly today, int staleAfterDays) =>
        IsStale(today, staleAfterDays) || IsNextActionDue(today);
}
