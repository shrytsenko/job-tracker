using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class ApplicationTests
{
    [Fact]
    public void Constructor_WithValidData_SetsPropertiesAndDefaultsStatusToInterested()
    {
        var application = new Application("Acme Corp", "Backend Engineer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent);

        Assert.Equal("Acme Corp", application.Company);
        Assert.Equal("Backend Engineer", application.RoleTitle);
        Assert.Equal(Track.Developer, application.Track);
        Assert.Equal(Channel.LinkedIn, application.Channel);
        Assert.Equal(EmploymentType.Permanent, application.EmploymentType);
        Assert.Equal(ApplicationStatus.Interested, application.Status);
        Assert.NotEqual(Guid.Empty, application.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyCompany_Throws(string? company)
    {
        Assert.Throws<ArgumentException>(() =>
            new Application(company!, "Backend Engineer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyRoleTitle_Throws(string? roleTitle)
    {
        Assert.Throws<ArgumentException>(() =>
            new Application("Acme Corp", roleTitle!, Track.Developer, Channel.LinkedIn, EmploymentType.Permanent));
    }

    [Fact]
    public void UpdateDetails_WithValidData_ReplacesRequiredFields()
    {
        var application = CreateApplication();

        application.UpdateDetails("Globex", "SDET", Track.SDET, Channel.Referral, EmploymentType.Contract);

        Assert.Equal("Globex", application.Company);
        Assert.Equal("SDET", application.RoleTitle);
        Assert.Equal(Track.SDET, application.Track);
        Assert.Equal(Channel.Referral, application.Channel);
        Assert.Equal(EmploymentType.Contract, application.EmploymentType);
    }

    [Fact]
    public void UpdateDetails_WithEmptyCompany_Throws()
    {
        var application = CreateApplication();

        Assert.Throws<ArgumentException>(() =>
            application.UpdateDetails(" ", "SDET", Track.SDET, Channel.Referral, EmploymentType.Contract));
    }

    [Fact]
    public void ChangeStatus_ToApplied_SetsAppliedOnWhenEmpty()
    {
        var application = CreateApplication();
        var today = new DateOnly(2026, 11, 9);

        application.ChangeStatus(ApplicationStatus.Applied, today);

        Assert.Equal(ApplicationStatus.Applied, application.Status);
        Assert.Equal(today, application.AppliedOn);
    }

    [Fact]
    public void ChangeStatus_ToApplied_KeepsExistingAppliedOn()
    {
        var application = CreateApplication();
        var originalDate = new DateOnly(2026, 11, 1);
        application.AppliedOn = originalDate;

        application.ChangeStatus(ApplicationStatus.Applied, new DateOnly(2026, 11, 9));

        Assert.Equal(originalDate, application.AppliedOn);
    }

    [Fact]
    public void ChangeStatus_ToOtherStatus_DoesNotSetAppliedOn()
    {
        var application = CreateApplication();

        application.ChangeStatus(ApplicationStatus.Rejected, new DateOnly(2026, 11, 9));

        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.Null(application.AppliedOn);
    }

    [Fact]
    public void AddActivity_AppendsToTimelineLinkedToApplication()
    {
        var application = CreateApplication();

        var activity = application.AddActivity(ActivityType.MessageSent, "Sent CV", new DateOnly(2026, 11, 9));

        Assert.Single(application.Activities, activity);
        Assert.Equal(application.Id, activity.ApplicationId);
    }

    [Theory]
    [InlineData(ActivityType.MessageSent)]
    [InlineData(ActivityType.ReplyReceived)]
    [InlineData(ActivityType.Interview)]
    public void AddActivity_RealInteraction_UpdatesLastContactOn(ActivityType type)
    {
        var application = CreateApplication();
        var date = new DateOnly(2026, 11, 9);

        application.AddActivity(type, "Something happened", date);

        Assert.Equal(date, application.LastContactOn);
    }

    [Fact]
    public void AddActivity_Note_DoesNotUpdateLastContactOn()
    {
        var application = CreateApplication();

        application.AddActivity(ActivityType.Note, "Private thought", new DateOnly(2026, 11, 9));

        Assert.Null(application.LastContactOn);
    }

    [Fact]
    public void AddActivity_OlderThanLastContact_DoesNotMoveLastContactBackwards()
    {
        var application = CreateApplication();
        var recent = new DateOnly(2026, 11, 9);
        application.LastContactOn = recent;

        application.AddActivity(ActivityType.ReplyReceived, "Backfilled old reply", new DateOnly(2026, 11, 1));

        Assert.Equal(recent, application.LastContactOn);
    }

    private static Application CreateApplication() =>
        new("Acme Corp", "Backend Engineer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent);
}
