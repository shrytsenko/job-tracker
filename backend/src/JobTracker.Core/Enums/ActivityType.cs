namespace JobTracker.Core.Enums;

public enum ActivityType
{
    MessageSent,
    ReplyReceived,
    Interview,
    Note,

    /// <summary>A take-home assignment (test task) was received. Track its deadline as the next action.</summary>
    TakeHome
}
