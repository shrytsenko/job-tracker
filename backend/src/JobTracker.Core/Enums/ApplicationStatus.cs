namespace JobTracker.Core.Enums;

/// <summary>
/// Where an application stands. <see cref="Applied"/> to <see cref="Offer"/> is the pipeline in order
/// (see <see cref="Entities.Application.PipelineStages"/>); the last four close the application.
/// </summary>
public enum ApplicationStatus
{
    /// <summary>Saved to look at, not applied to yet. Comes before the pipeline.</summary>
    Interested,

    /// <summary>Application sent.</summary>
    Applied,

    /// <summary>First call with a recruiter or HR.</summary>
    RecruiterScreen,

    /// <summary>Technical interview or test task.</summary>
    TechInterview,

    /// <summary>Final round, typically with the hiring manager or team.</summary>
    FinalInterview,

    /// <summary>An offer was made.</summary>
    Offer,

    /// <summary>Closed: the offer was accepted.</summary>
    Accepted,

    /// <summary>Closed: the company said no.</summary>
    Rejected,

    /// <summary>Closed: the candidate stepped back.</summary>
    Withdrawn,

    /// <summary>Closed: the company never answered.</summary>
    NoResponse
}
