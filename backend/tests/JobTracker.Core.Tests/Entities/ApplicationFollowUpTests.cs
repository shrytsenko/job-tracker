using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class ApplicationFollowUpTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);
    private const int StaleAfterDays = 5;

    [Theory]
    [InlineData(5, true)]   // exactly N days of silence counts
    [InlineData(6, true)]
    [InlineData(4, false)]
    public void IsStale_InProgress_DependsOnDaysSinceLastContact(int daysAgo, bool expected)
    {
        var application = CreateWithStatus(ApplicationStatus.RecruiterScreen);
        application.LastContactOn = Today.AddDays(-daysAgo);

        Assert.Equal(expected, application.IsStale(Today, StaleAfterDays));
    }

    [Fact]
    public void IsStale_NoContactYet_FallsBackToAppliedOn()
    {
        var application = CreateWithStatus(ApplicationStatus.Applied);
        application.AppliedOn = Today.AddDays(-10);

        Assert.True(application.IsStale(Today, StaleAfterDays));
        Assert.Equal(10, application.DaysSinceContact(Today));
    }

    [Fact]
    public void IsStale_NoDatesAtAll_IsFalse()
    {
        var application = CreateWithStatus(ApplicationStatus.TechInterview);

        Assert.Null(application.DaysSinceContact(Today));
        Assert.False(application.IsStale(Today, StaleAfterDays));
    }

    [Theory]
    [InlineData(ApplicationStatus.Interested)]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Accepted)]
    [InlineData(ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.NoResponse)]
    public void IsStale_NotInProgress_IsFalseEvenAfterLongSilence(ApplicationStatus status)
    {
        var application = CreateWithStatus(status);
        application.LastContactOn = Today.AddDays(-30);

        Assert.False(application.IsStale(Today, StaleAfterDays));
    }

    [Theory]
    [InlineData(-1, true)]  // overdue
    [InlineData(0, true)]   // due today
    [InlineData(1, false)]  // due tomorrow
    public void IsNextActionDue_DependsOnDueDate(int dueInDays, bool expected)
    {
        var application = CreateWithStatus(ApplicationStatus.Interested);
        application.SetNextAction("Follow up", Today.AddDays(dueInDays));

        Assert.Equal(expected, application.IsNextActionDue(Today));
    }

    [Fact]
    public void IsNextActionDue_ClosedApplication_IsFalse()
    {
        var application = CreateWithStatus(ApplicationStatus.Rejected);
        application.SetNextAction("Follow up", Today.AddDays(-3));

        Assert.False(application.IsNextActionDue(Today));
    }

    [Fact]
    public void NeedsFollowUp_FreshContactAndNoDueAction_IsFalse()
    {
        var application = CreateWithStatus(ApplicationStatus.Applied);
        application.LastContactOn = Today.AddDays(-1);
        application.SetNextAction("Follow up", Today.AddDays(3));

        Assert.False(application.NeedsFollowUp(Today, StaleAfterDays));
    }

    [Fact]
    public void NeedsFollowUp_StaleOrDue_IsTrue()
    {
        var stale = CreateWithStatus(ApplicationStatus.Applied);
        stale.LastContactOn = Today.AddDays(-7);

        var due = CreateWithStatus(ApplicationStatus.Interested);
        due.SetNextAction("Follow up", Today);

        Assert.True(stale.NeedsFollowUp(Today, StaleAfterDays));
        Assert.True(due.NeedsFollowUp(Today, StaleAfterDays));
    }

    private static Application CreateWithStatus(ApplicationStatus status)
    {
        var application = new Application("Acme Corp", "Backend Engineer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent);
        application.ChangeStatus(status, Today);
        application.AppliedOn = null; // isolate each test from ChangeStatus stamping the applied date
        return application;
    }
}
