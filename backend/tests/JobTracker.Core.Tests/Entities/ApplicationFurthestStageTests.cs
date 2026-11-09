using JobTracker.Core.Entities;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Tests.Entities;

public class ApplicationFurthestStageTests
{
    private static readonly DateOnly Today = new(2026, 11, 9);

    [Fact]
    public void NewApplication_HasNoFurthestStage()
    {
        Assert.Null(CreateApplication().FurthestStage);
    }

    [Theory]
    [InlineData(ApplicationStatus.Applied)]
    [InlineData(ApplicationStatus.RecruiterScreen)]
    [InlineData(ApplicationStatus.TechInterview)]
    [InlineData(ApplicationStatus.FinalInterview)]
    [InlineData(ApplicationStatus.Offer)]
    public void ChangeStatus_ToPipelineStage_RecordsThatStage(ApplicationStatus stage)
    {
        var application = CreateApplication();

        application.ChangeStatus(stage, Today);

        Assert.Equal(stage, application.FurthestStage);
    }

    [Theory]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.NoResponse)]
    public void ChangeStatus_ToClosingStatus_KeepsStageWhereItEnded(ApplicationStatus closing)
    {
        var application = CreateApplication();
        application.ChangeStatus(ApplicationStatus.TechInterview, Today);

        application.ChangeStatus(closing, Today);

        Assert.Equal(ApplicationStatus.TechInterview, application.FurthestStage);
    }

    [Fact]
    public void ChangeStatus_ToAccepted_CountsAsReachingOffer()
    {
        var application = CreateApplication();
        application.ChangeStatus(ApplicationStatus.FinalInterview, Today);

        application.ChangeStatus(ApplicationStatus.Accepted, Today);

        Assert.Equal(ApplicationStatus.Offer, application.FurthestStage);
    }

    [Fact]
    public void ChangeStatus_BackToEarlierStage_TreatsItAsACorrection()
    {
        var application = CreateApplication();
        application.ChangeStatus(ApplicationStatus.FinalInterview, Today);

        application.ChangeStatus(ApplicationStatus.TechInterview, Today);

        Assert.Equal(ApplicationStatus.TechInterview, application.FurthestStage);
    }

    [Fact]
    public void ChangeStatus_BackToInterested_ClearsFurthestStage()
    {
        var application = CreateApplication();
        application.ChangeStatus(ApplicationStatus.Applied, Today);

        application.ChangeStatus(ApplicationStatus.Interested, Today);

        Assert.Null(application.FurthestStage);
    }

    private static Application CreateApplication() =>
        new("Acme Corp", "Backend Engineer", Track.Developer, Channel.LinkedIn, EmploymentType.Permanent);
}
