using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class JobDescriptionAndProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SetJobDescription_NewText_ClearsStaleAnalysis()
    {
        var application = CreateApplicationWithAnalysis("Old post");

        application.SetJobDescription("A different post");

        Assert.Equal("A different post", application.JobDescription);
        Assert.Null(application.Analysis);
    }

    [Fact]
    public void SetJobDescription_SameText_KeepsAnalysis()
    {
        var application = CreateApplicationWithAnalysis("Same post");

        application.SetJobDescription("  Same post  ");

        Assert.NotNull(application.Analysis);
    }

    [Fact]
    public void SetJobDescription_TooLong_Throws()
    {
        var application = CreateApplication();

        Assert.Throws<ArgumentException>(() => application.SetJobDescription(new string('x', Application.MaxJobDescriptionLength + 1)));
    }

    [Fact]
    public void RecordAnalysis_WithoutJobDescription_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => CreateApplication().RecordAnalysis(Analysis(50)));
    }

    [Fact]
    public void RecordAnalysis_ReplacesPreviousAnalysis()
    {
        var application = CreateApplicationWithAnalysis("Post");

        application.RecordAnalysis(Analysis(90));

        Assert.Equal(90, application.Analysis!.MatchScore);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void JobAnalysisCreate_ScoreOutOfRange_Throws(int score)
    {
        Assert.Throws<ArgumentException>(() => Analysis(score));
    }

    [Fact]
    public void JobAnalysisCreate_TreatsTheWordNullAsMissing()
    {
        var analysis = JobAnalysis.Create([], "null", " ", null, "NULL", 50, [], [], [], "model", Now);

        Assert.Null(analysis.Seniority);
        Assert.Null(analysis.LocationRestrictions);
        Assert.Null(analysis.Salary);
    }

    [Fact]
    public void JobAnalysisCreate_CapsListSize()
    {
        var many = Enumerable.Range(1, 100).Select(i => $"skill {i}");

        var analysis = JobAnalysis.Create(many, null, null, null, null, 50, [], [], [], "model", Now);

        Assert.Equal(JobAnalysis.MaxItems, analysis.Stack.Count);
    }

    [Fact]
    public void Profile_UpdateCvSummary_TrimsAndStampsTime()
    {
        var profile = new Profile();

        profile.UpdateCvSummary("  Senior .NET developer  ", Now);

        Assert.Equal("Senior .NET developer", profile.CvSummary);
        Assert.Equal(Now, profile.UpdatedAt);
        Assert.True(profile.HasCvSummary);
    }

    [Fact]
    public void Profile_UpdateCvSummary_TooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Profile().UpdateCvSummary(new string('x', Profile.MaxCvSummaryLength + 1), Now));
    }

    private static Application CreateApplication() =>
        new("Contoso", "Developer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent);

    private static Application CreateApplicationWithAnalysis(string jobDescription)
    {
        var application = CreateApplication();
        application.SetJobDescription(jobDescription);
        application.RecordAnalysis(Analysis(70));
        return application;
    }

    private static JobAnalysis Analysis(int score) =>
        JobAnalysis.Create(["C#"], "Senior", null, null, null, score, [], [], [], "model", Now);
}
