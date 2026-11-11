namespace JobTracker.Core.Entities;

/// <summary>
/// The tracker's single user profile: the CV summary the AI compares job posts against, and the name
/// reply drafts are signed with.
/// </summary>
public class Profile
{
    /// <summary>There is exactly one profile, so it always has this id.</summary>
    public const int SingletonId = 1;
    /// <summary>Longest allowed <see cref="CvSummary"/>, which keeps the AI prompt (and its cost) bounded.</summary>
    public const int MaxCvSummaryLength = 8000;

    /// <summary>Longest allowed <see cref="FullName"/>.</summary>
    public const int MaxFullNameLength = 100;

    /// <summary>Always <see cref="SingletonId"/>.</summary>
    public int Id { get; private set; } = SingletonId;

    /// <summary>The candidate's experience and skills in plain text; empty until set.</summary>
    public string CvSummary { get; private set; } = string.Empty;

    /// <summary>The name reply drafts are signed with; null until set.</summary>
    public string? FullName { get; private set; }

    /// <summary>When the profile was last saved.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Whether a CV summary is set. A job analysis needs one to compute a match score.</summary>
    public bool HasCvSummary => !string.IsNullOrWhiteSpace(CvSummary);

    /// <summary>Replaces both fields. Values are trimmed, and a blank name is stored as null.</summary>
    /// <exception cref="ArgumentException">A value is longer than its limit.</exception>
    public void Update(string? cvSummary, string? fullName, DateTimeOffset now)
    {
        var summary = cvSummary?.Trim() ?? string.Empty;
        if (summary.Length > MaxCvSummaryLength)
        {
            throw new ArgumentException($"CV summary can be at most {MaxCvSummaryLength} characters.", nameof(cvSummary));
        }

        var name = string.IsNullOrWhiteSpace(fullName) ? null : fullName.Trim();
        if (name?.Length > MaxFullNameLength)
        {
            throw new ArgumentException($"Name can be at most {MaxFullNameLength} characters.", nameof(fullName));
        }

        CvSummary = summary;
        FullName = name;
        UpdatedAt = now;
    }
}
