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
    public string? CompensationCurrency { get; set; }
    public CompensationPeriod? CompensationPeriod { get; set; }
    public bool? CompensationIsNet { get; set; }

    public DateOnly? AppliedOn { get; set; }
    public DateOnly? LastContactOn { get; set; }
    public string? NextActionDescription { get; set; }
    public DateOnly? NextActionDueOn { get; set; }
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
}
