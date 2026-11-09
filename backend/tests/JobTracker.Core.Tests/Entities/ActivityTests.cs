using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class ActivityTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var applicationId = Guid.NewGuid();
        var date = new DateOnly(2026, 11, 9);

        var activity = new Activity(applicationId, ActivityType.MessageSent, "Sent intro message", date);

        Assert.Equal(applicationId, activity.ApplicationId);
        Assert.Equal(ActivityType.MessageSent, activity.Type);
        Assert.Equal("Sent intro message", activity.Text);
        Assert.Equal(date, activity.Date);
        Assert.NotEqual(Guid.Empty, activity.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyText_Throws(string? text)
    {
        Assert.Throws<ArgumentException>(() =>
            new Activity(Guid.NewGuid(), ActivityType.Note, text!, DateOnly.FromDateTime(DateTime.UtcNow)));
    }
}
