using JobTracker.Core.Ai;

namespace JobTracker.Core.Tests.Ai;

public class RestrictionScannerTests
{
    [Theory]
    [InlineData("This role is US only.")]
    [InlineData("Remote (USA)")]
    [InlineData("Remote - United States only")]
    [InlineData("You must be located in the US.")]
    [InlineData("Candidates must reside in the United States")]
    [InlineData("Must be authorized to work in the United States")]
    [InlineData("Applicants must be eligible to work in the U.S. without sponsorship")]
    [InlineData("US work authorization is required")]
    [InlineData("W2 only, no C2C")]
    [InlineData("US citizens or green card holders")]
    public void Scan_UsRestrictions_AreFlagged(string text)
    {
        Assert.Contains(RestrictionScanner.Scan(text), flag => flag.Contains("US"));
    }

    [Theory]
    [InlineData("We do not offer visa sponsorship.")]
    [InlineData("Unable to sponsor visas at this time")]
    [InlineData("Sponsorship is not available")]
    public void Scan_NoSponsorship_IsFlagged(string text)
    {
        Assert.Contains(RestrictionScanner.Scan(text), flag => flag.Contains("sponsorship"));
    }

    [Theory]
    [InlineData("UK only")]
    [InlineData("You must have the right to work in the UK")]
    public void Scan_UkRestrictions_AreFlagged(string text)
    {
        Assert.Contains(RestrictionScanner.Scan(text), flag => flag.Contains("UK"));
    }

    [Fact]
    public void Scan_SecurityClearance_IsFlagged()
    {
        Assert.Contains(RestrictionScanner.Scan("Active SC clearance required"), flag => flag.Contains("clearance"));
    }

    [Theory]
    [InlineData("Remote across the EU. We work with US clients.")]
    [InlineData("Our customers include businesses in the United States and Europe.")]
    [InlineData("Must be comfortable with async status updates.")]
    [InlineData("Hybrid in Barcelona, 2 days per week in the office.")]
    [InlineData("We sponsor conference trips every year.")]
    public void Scan_OrdinaryText_IsNotFlagged(string text)
    {
        Assert.Empty(RestrictionScanner.Scan(text));
    }
}
