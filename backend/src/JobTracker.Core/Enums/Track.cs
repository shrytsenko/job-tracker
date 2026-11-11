namespace JobTracker.Core.Enums;

/// <summary>
/// The career track an application belongs to. The dashboard compares the funnels per track to decide where
/// to focus the search.
/// </summary>
public enum Track
{
    /// <summary>Software developer roles (the main track).</summary>
    Developer,

    /// <summary>Test automation and SDET roles (the backup track).</summary>
    SDET,

    /// <summary>Integration engineering roles.</summary>
    Integration,

    /// <summary>Anything else.</summary>
    Other
}
