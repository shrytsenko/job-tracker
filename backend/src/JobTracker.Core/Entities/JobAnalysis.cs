namespace JobTracker.Core.Entities;

/// <summary>
/// The AI's reading of a job description against the candidate's CV. Immutable: re-analysing an application
/// replaces the whole analysis. Built only through <see cref="Create"/>, which validates and normalises the
/// model's output, so nothing unchecked from the LLM reaches the database.
/// </summary>
public sealed class JobAnalysis
{
    public const int MaxItems = 25;
    public const int MaxItemLength = 300;
    public const int MaxFieldLength = 300;

    public IReadOnlyList<string> Stack { get; private set; } = [];
    public string? Seniority { get; private set; }
    public string? LocationRestrictions { get; private set; }
    public string? EmploymentType { get; private set; }
    public string? Salary { get; private set; }
    public int MatchScore { get; private set; }
    public IReadOnlyList<string> Strengths { get; private set; } = [];
    public IReadOnlyList<string> Gaps { get; private set; } = [];
    public IReadOnlyList<string> RedFlags { get; private set; } = [];
    public string Model { get; private set; } = null!;
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
