namespace JobTracker.Core.Entities;

/// <summary>
/// The AI's reading of a job description against the candidate's CV. Immutable: re-analysing an application
/// replaces the whole analysis. Built only through <see cref="Create"/>, which validates and normalises the
/// model's output, so nothing unchecked from the LLM reaches the database.
/// </summary>
public sealed class JobAnalysis
{
    /// <summary>Most entries kept in each list; extra ones are dropped.</summary>
    public const int MaxItems = 25;

    /// <summary>Longest list entry; longer ones are cut.</summary>
    public const int MaxItemLength = 300;

    /// <summary>Longest single-value field; longer ones are cut.</summary>
    public const int MaxFieldLength = 300;

    /// <summary>Technologies the post asks for, e.g. "C#", "ASP.NET Core", "Azure".</summary>
    public IReadOnlyList<string> Stack { get; private set; } = [];

    /// <summary>Seniority as stated in the post; null when it doesn't say.</summary>
    public string? Seniority { get; private set; }

    /// <summary>Location or work-authorisation limits, e.g. "Remote, EU only"; null when none are stated.</summary>
    public string? LocationRestrictions { get; private set; }

    /// <summary>Employment type in the post's own words; free text, unlike <see cref="Enums.EmploymentType"/>.</summary>
    public string? EmploymentType { get; private set; }

    /// <summary>The salary or rate if the post states one, as written.</summary>
    public string? Salary { get; private set; }

    /// <summary>How well the CV matches the post, from 0 (no match) to 100.</summary>
    public int MatchScore { get; private set; }

    /// <summary>Where the CV matches the post well.</summary>
    public IReadOnlyList<string> Strengths { get; private set; } = [];

    /// <summary>Requirements the CV does not show.</summary>
    public IReadOnlyList<string> Gaps { get; private set; } = [];

    /// <summary>Deal-breakers to check before applying, e.g. "US work authorization required".</summary>
    public IReadOnlyList<string> RedFlags { get; private set; } = [];

    /// <summary>The model that produced the analysis, as reported by the provider.</summary>
    public string Model { get; private set; } = null!;

    /// <summary>When the analysis was made.</summary>
    public DateTimeOffset AnalyzedAt { get; private set; }

    private JobAnalysis()
    {
    }

    /// <exception cref="ArgumentException">The values break a rule (score outside 0–100, missing model name).</exception>
    public static JobAnalysis Create(
        IEnumerable<string>? stack,
        string? seniority,
        string? locationRestrictions,
        string? employmentType,
        string? salary,
        int matchScore,
        IEnumerable<string>? strengths,
        IEnumerable<string>? gaps,
        IEnumerable<string>? redFlags,
        string model,
        DateTimeOffset analyzedAt)
    {
        if (matchScore is < 0 or > 100)
        {
            throw new ArgumentException("Match score must be between 0 and 100.", nameof(matchScore));
        }

        return new JobAnalysis
        {
            Stack = CleanList(stack),
            Seniority = CleanField(seniority),
            LocationRestrictions = CleanField(locationRestrictions),
            EmploymentType = CleanField(employmentType),
            Salary = CleanField(salary),
            MatchScore = matchScore,
            Strengths = CleanList(strengths),
            Gaps = CleanList(gaps),
            RedFlags = CleanList(redFlags),
            Model = Guard.AgainstNullOrWhiteSpace(model, nameof(model)),
            AnalyzedAt = analyzedAt,
        };
    }

    /// <summary>Adds a red flag unless an equivalent one is already listed (case-insensitive).</summary>
    public JobAnalysis WithRedFlag(string redFlag)
    {
        if (RedFlags.Any(f => f.Equals(redFlag, StringComparison.OrdinalIgnoreCase)))
        {
            return this;
        }

        var copy = (JobAnalysis)MemberwiseClone();
        copy.RedFlags = CleanList([.. RedFlags, redFlag]);
        return copy;
    }

    // Models occasionally return blanks, duplicates or very long items; keep the stored data tidy and bounded.
    private static IReadOnlyList<string> CleanList(IEnumerable<string>? items) =>
        (items ?? [])
            .Select(i => i?.Trim() ?? string.Empty)
            .Where(i => i.Length > 0)
            .Select(i => i.Length > MaxItemLength ? i[..MaxItemLength] : i)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxItems)
            .ToList();

    private static string? CleanField(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmed.Length > MaxFieldLength ? trimmed[..MaxFieldLength] : trimmed;
    }
}
