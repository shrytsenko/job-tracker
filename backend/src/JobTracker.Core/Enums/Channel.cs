namespace JobTracker.Core.Enums;

/// <summary>Where the application came from or was sent through, to see which channels work.</summary>
public enum Channel
{
    /// <summary>LinkedIn job post or recruiter message.</summary>
    LinkedIn,

    /// <summary>Djinni, the Ukrainian job board.</summary>
    Djinni,

    /// <summary>Direct email.</summary>
    Email,

    /// <summary>Through a referral by someone at the company.</summary>
    Referral,

    /// <summary>A freelance or talent platform such as Upwork, Shakers or Toptal.</summary>
    Platform,

    /// <summary>The company's own careers page.</summary>
    CompanyWebsite,

    /// <summary>Any other source.</summary>
    Other
}
