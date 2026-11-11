namespace JobTracker.Core.Enums;

/// <summary>
/// The kind of timeline entry. Everything except <see cref="Note"/> is a real interaction and counts as contact
/// for the follow-up rule.
/// </summary>
public enum ActivityType
{
    /// <summary>The candidate sent a message (LinkedIn, email, platform chat).</summary>
    MessageSent,

    /// <summary>The company or recruiter replied.</summary>
    ReplyReceived,

    /// <summary>An interview or call took place.</summary>
    Interview,

    /// <summary>A private note. Not contact, so it does not move the last contact date.</summary>
    Note,

    /// <summary>A take-home assignment (test task) was received. Track its deadline as the next action.</summary>
    TakeHome
}
