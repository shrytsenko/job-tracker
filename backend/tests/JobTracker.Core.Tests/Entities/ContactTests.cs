using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class ContactTests
{
    [Fact]
    public void Constructor_WithValidData_SetsProperties()
    {
        var contact = new Contact("Jane Recruiter", ContactRole.Recruiter);

        Assert.Equal("Jane Recruiter", contact.Name);
        Assert.Equal(ContactRole.Recruiter, contact.Role);
        Assert.NotEqual(Guid.Empty, contact.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Contact(name!, ContactRole.Recruiter));
    }
}
