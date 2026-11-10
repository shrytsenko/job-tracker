using System.Text.RegularExpressions;

namespace JobTracker.Core.Ai;

/// <summary>
/// A deterministic backstop for the one thing the analysis must never miss: posts limited to candidates in,
/// or authorised to work in, a specific country. The model handles nuance; this guarantees the obvious
/// phrasings ("US only", "must be authorized to work in the United States", "W2 only") are always flagged.
/// </summary>
public static partial class RestrictionScanner
{
    private sealed record Rule(Regex Pattern, string RedFlag);

    // Note: patterns end country names with (?![A-Za-z]) rather than \b, because "U.S." ends in a dot and
    // "\b" finds no word boundary between a dot and the following space.
    private static readonly Rule[] Rules =
    [
        new(UsOnly(), "Restricted to the US: candidates must be US-based or authorised to work in the US"),
        new(UsWorkAuthorization(), "Requires US work authorisation"),
        new(UsEmploymentType(), "US-only employment type (W2 / US citizen or green card)"),
        new(NoSponsorship(), "No visa sponsorship offered"),
        new(UkOnly(), "Restricted to the UK: right to work in the UK required"),
        new(SecurityClearance(), "Requires a security clearance"),
    ];

    /// <summary>Red flags for every restriction rule the text matches.</summary>
    public static IReadOnlyList<string> Scan(string jobDescription) =>
        Rules.Where(rule => rule.Pattern.IsMatch(jobDescription)).Select(rule => rule.RedFlag).ToList();

    [GeneratedRegex(@"\b(US|USA|U\.S\.|United States)[\s-]*(only|based\s+only|residents?\s+only)\b|\b(must|need\s+to|required\s+to)\s+(be\s+)?(located|based|reside|residing|live)\s+in\s+the\s+(US|USA|U\.S\.|United States)(?![A-Za-z])|\bremote\s*\(\s*(US|USA|United States)\s*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UsOnly();

    [GeneratedRegex(@"\b(authori[sz]ed|authori[sz]ation|eligible)\s+to\s+work\s+in\s+the\s+(US|USA|U\.S\.|United States)(?![A-Za-z])|\bUS\s+work\s+authori[sz]ation\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UsWorkAuthorization();

    [GeneratedRegex(@"\bW-?2\b(?!\s*form)|\b(US|U\.S\.)\s+citizens?(hip)?\b|\bgreen\s+card\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UsEmploymentType();

    [GeneratedRegex(@"\b(no|not|unable\s+to|cannot|can't|do\s+not|does\s+not|will\s+not)\s+(offer\s+|provide\s+)?(visa\s+)?sponsor(ship)?\b|\bsponsorship\s+(is\s+)?not\s+(available|offered|provided)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NoSponsorship();

    [GeneratedRegex(@"\b(UK|United Kingdom)[\s-]*(only|based\s+only)\b|\bright\s+to\s+work\s+in\s+the\s+(UK|United Kingdom)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UkOnly();

    [GeneratedRegex(@"\b(security|SC|DV|secret|top\s+secret|TS/SCI)\s+clearance\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SecurityClearance();
}
