namespace JobTracker.Core.Entities;

/// <summary>
/// The tracker's single user profile: the CV summary the AI compares job posts against, and the name
/// reply drafts are signed with.
/// </summary>
public class Profile
{
    /// <summary>There is exactly one profile, so it always has this id.</summary>
    public const int SingletonId = 1;
    public const int MaxCvSummaryLength = 8000;
    public const int MaxFullNameLength = 100;

    public int Id { get; private set; } = SingletonId;
    public string CvSummary { get; private set; } = string.Empty;
    public string? FullName { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasCvSummary => !string.IsNullOrWhiteSpace(CvSummary);

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
