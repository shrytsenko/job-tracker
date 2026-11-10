namespace JobTracker.Core.Entities;

/// <summary>The tracker's single user profile. Holds the CV summary the AI compares job posts against.</summary>
public class Profile
{
    /// <summary>There is exactly one profile, so it always has this id.</summary>
    public const int SingletonId = 1;
    public const int MaxCvSummaryLength = 8000;

    public int Id { get; private set; } = SingletonId;
    public string CvSummary { get; private set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasCvSummary => !string.IsNullOrWhiteSpace(CvSummary);

    public void UpdateCvSummary(string? cvSummary, DateTimeOffset now)
    {
        var trimmed = cvSummary?.Trim() ?? string.Empty;
        if (trimmed.Length > MaxCvSummaryLength)
        {
            throw new ArgumentException($"CV summary can be at most {MaxCvSummaryLength} characters.", nameof(cvSummary));
        }

        CvSummary = trimmed;
        UpdatedAt = now;
    }
}
