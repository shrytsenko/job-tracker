using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>Immutable append-only log entry — Phase 1 only ever adds activities, never edits them.</summary>
public class Activity
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ApplicationId { get; private set; }
    public ActivityType Type { get; private set; }
    public string Text { get; private set; } = null!;
    public DateOnly Date { get; private set; }

    private Activity()
    {
    }

    public Activity(Guid applicationId, ActivityType type, string text, DateOnly date)
    {
        ApplicationId = applicationId;
        Type = type;
        Text = Guard.AgainstNullOrWhiteSpace(text, nameof(text));
        Date = date;
    }
}
