using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class ApplicationNextActionTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);

    [Fact]
    public void SetNextAction_WithDescriptionAndDate_SetsBothTrimmed()
    {
        var application = CreateApplication();

        application.SetNextAction("  Message Dana  ", Today);

        Assert.Equal("Message Dana", application.NextActionDescription);
        Assert.Equal(Today, application.NextActionDueOn);
    }

    [Fact]
    public void SetNextAction_WithDescriptionOnly_AllowsNoDueDate()
    {
        var application = CreateApplication();

        application.SetNextAction("Read about the company", null);

        Assert.Equal("Read about the company", application.NextActionDescription);
        Assert.Null(application.NextActionDueOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetNextAction_EmptyDescriptionAndNoDate_ClearsNextAction(string? description)
    {
        var application = CreateApplication();
        application.SetNextAction("Message Dana", Today);

        application.SetNextAction(description, null);

        Assert.Null(application.NextActionDescription);
        Assert.Null(application.NextActionDueOn);
    }

    [Fact]
    public void SetNextAction_DueDateWithoutDescription_Throws()
    {
        var application = CreateApplication();

        Assert.Throws<ArgumentException>(() => application.SetNextAction(" ", Today));
    }

    [Fact]
    public void CompleteNextAction_LogsDoneActivityAndClearsNextAction()
    {
        var application = CreateApplication();
        application.SetNextAction("Message Dana", Today.AddDays(-14));

        var activity = application.CompleteNextAction(ActivityType.Note, Today);

        Assert.Equal("Done: Message Dana", activity.Text);
        Assert.Equal(ActivityType.Note, activity.Type);
        Assert.Equal(Today, activity.Date);
        Assert.Contains(activity, application.Activities);
        Assert.Null(application.NextActionDescription);
        Assert.Null(application.NextActionDueOn);
    }

    [Fact]
    public void CompleteNextAction_AsNote_DoesNotCountAsContact()
    {
        var application = CreateApplication();
        application.SetNextAction("Update CV", null);

        application.CompleteNextAction(ActivityType.Note, Today);

        Assert.Null(application.LastContactOn);
    }

    [Fact]
    public void CompleteNextAction_AsMessageSent_CountsAsContact()
    {
        var application = CreateApplication();
        application.SetNextAction("Message Dana", null);

        application.CompleteNextAction(ActivityType.MessageSent, Today);

        Assert.Equal(Today, application.LastContactOn);
    }

    [Fact]
    public void CompleteNextAction_WithoutNextAction_Throws()
    {
        var application = CreateApplication();

        Assert.Throws<InvalidOperationException>(() => application.CompleteNextAction(ActivityType.Note, Today));
    }

    private static Application CreateApplication() =>
        new("Acme Corp", "Backend Engineer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent);
}
