using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

public class Application
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Company { get; private set; } = null!;
    public string RoleTitle { get; private set; } = null!;
    public string? JobUrl { get; set; }
    public Track Track { get; private set; }
    public Channel Channel { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Interested;

    public string? RemoteRegion { get; set; }
    public string? HybridCity { get; set; }

    public decimal? CompensationAmount { get; set; }
    public Currency? CompensationCurrency { get; set; }
    public CompensationPeriod? CompensationPeriod { get; set; }
    public bool? CompensationIsNet { get; set; }

    public DateOnly? AppliedOn { get; set; }
    public DateOnly? LastContactOn { get; set; }
    public string? NextActionDescription { get; private set; }
    public DateOnly? NextActionDueOn { get; private set; }
    public string? Notes { get; set; }

    public List<Contact> Contacts { get; private set; } = new();
    public List<Activity> Activities { get; private set; } = new();

    private Application()
    {
    }

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

    public bool NeedsFollowUp(DateOnly today, int staleAfterDays) =>
        IsStale(today, staleAfterDays) || IsNextActionDue(today);
}
