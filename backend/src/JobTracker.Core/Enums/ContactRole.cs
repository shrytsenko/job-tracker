namespace JobTracker.Core.Enums;

/// <summary>How a contact relates to the job search.</summary>
public enum ContactRole
{
    /// <summary>An in-house or agency recruiter.</summary>
    Recruiter,

    /// <summary>Someone who can refer the candidate.</summary>
    Referrer,

    /// <summary>A former colleague.</summary>
    ExColleague,

    /// <summary>The manager the role reports to.</summary>
    HiringManager,

    /// <summary>Anyone else.</summary>
    Other
}
