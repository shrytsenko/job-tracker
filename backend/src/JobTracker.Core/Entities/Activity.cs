using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>Immutable append-only log entry — Phase 1 only ever adds activities, never edits them.</summary>
public class Activity
{
    /// <summary>Unique id, generated on creation.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>The application whose timeline this entry belongs to.</summary>
    public Guid ApplicationId { get; private set; }

    /// <summary>What happened. Decides whether the entry counts as contact.</summary>
    public ActivityType Type { get; private set; }

    /// <summary>A short description, e.g. "Sent CV to the recruiter". Never empty.</summary>
    public string Text { get; private set; } = null!;

    /// <summary>The day it happened, which can be earlier than the day it was logged.</summary>
    public DateOnly Date { get; private set; }

    private Activity()
    {
    }

    /// <summary>Creates an entry. Use <see cref="Application.AddActivity"/> so the contact date is kept up to date.</summary>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    public Activity(Guid applicationId, ActivityType type, string text, DateOnly date)
    {
        ApplicationId = applicationId;
        Type = type;
        Text = Guard.AgainstNullOrWhiteSpace(text, nameof(text));
        Date = date;
    }
}
