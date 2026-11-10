using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Persistence;

/// <summary>
/// Fictional demo data for screenshots, manual testing and the dashboard tests. Companies are the
/// standard fictional names used in Microsoft documentation; emails use example.com. Only ever written
/// to an empty database (the "demo" launch profile uses a separate jobtracker.demo.db file).
///
/// Expected dashboard numbers (asserted by DashboardEndpointsTests):
///
///   Track        Total  Applied  Screen  Tech  Offer
///   Developer      10        9       6     4      1
///   SDET            8        7       4     2      0
///   Integration     2        2       1     0      0
///
///   By status: Interested 2, Applied 5, RecruiterScreen 3, TechInterview 2, FinalInterview 1,
///              Offer 1, Accepted 0, Rejected 3, Withdrawn 1, NoResponse 2  (total 20)
/// </summary>
public static class DemoData
{
    public static async Task<bool> SeedIfEmptyAsync(JobTrackerDbContext db, DateOnly today, CancellationToken ct = default)
    {
        if (await db.Applications.AnyAsync(ct) || await db.Contacts.AnyAsync(ct))
        {
            return false;
        }

        var recruiterDana = Contact("Dana Whitfield", ContactRole.Recruiter, "Northwind Traders", "dana.whitfield@example.com");
        var recruiterOmar = Contact("Omar Haddad", ContactRole.Recruiter, "Hays Talent (agency)", "omar.haddad@example.com");
        var referrerLena = Contact("Lena Kovac", ContactRole.ExColleague, "Woodgrove Bank", "lena.kovac@example.com");
        var managerRui = Contact("Rui Santos", ContactRole.HiringManager, "Lucerne Publishing", "rui.santos@example.com");

        var b = new Builder(today);
        var applications = new List<DemoApplication>
        {
            // Developer (10)
            b.App("Northwind Traders", "Senior .NET Developer", Track.Developer, Channel.LinkedIn, 26,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.TechInterview, ApplicationStatus.FinalInterview, ApplicationStatus.Offer)
                .With(a => { a.RemoteRegion = "Remote EU"; a.CompensationAmount = 62000; a.CompensationCurrency = Currency.EUR; a.CompensationPeriod = CompensationPeriod.Year; a.CompensationIsNet = false; })
                .Contact(recruiterDana).Log(ActivityType.ReplyReceived, "Offer received, decision by Friday", 1)
                .Next("Reply to the offer", 2),
            b.App("Contoso Cloud", "Backend Engineer (.NET)", Track.Developer, Channel.CompanyWebsite, 18,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.TechInterview)
                .With(a => a.HybridCity = "Barcelona")
                .Log(ActivityType.Interview, "Tech interview: EF Core and system design", 3)
                .Next("Send thank-you note and the code sample", 0),
            b.App("Fabrikam Payments", ".NET Developer", Track.Developer, Channel.Djinni, 24,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.TechInterview, ApplicationStatus.Rejected)
                .Log(ActivityType.ReplyReceived, "Rejected: looking for more payments domain experience", 6),
            b.App("Tailspin Travel", "C# Developer", Track.Developer, Channel.Referral, 9,
                ApplicationStatus.RecruiterScreen)
                .Contact(recruiterOmar).Log(ActivityType.ReplyReceived, "Recruiter call booked", 4)
                .Next("Prepare questions for the screen", 1),
            b.App("Adventure Works", "Senior Software Engineer", Track.Developer, Channel.LinkedIn, 21,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.Rejected),
            b.App("Wingtip Games", "Backend Developer", Track.Developer, Channel.LinkedIn, 3),
            b.App("Litware Health", ".NET Engineer", Track.Developer, Channel.Platform, 12)
                .With(a => a.RemoteRegion = "US only"),
            b.App("Proseware", "Software Engineer", Track.Developer, Channel.Email, 28,
                ApplicationStatus.NoResponse),
            b.App("Lucerne Publishing", ".NET Developer", Track.Developer, Channel.LinkedIn, 15,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.TechInterview, ApplicationStatus.FinalInterview)
                .Contact(managerRui).Log(ActivityType.Interview, "Final round with the team lead", 2)
                .Next("Follow up on the final interview", -1),
            b.Interested("Blue Yonder Airlines", "Senior .NET Engineer", Track.Developer, Channel.LinkedIn)
                .Next("Tailor the CV and apply", 3),

            // SDET (8)
            b.App("Woodgrove Bank", "SDET (C#, Playwright)", Track.SDET, Channel.Referral, 14,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.TechInterview)
                .Contact(referrerLena).Log(ActivityType.TakeHome, "Take-home: API test suite with Playwright", 4)
                .Next("Submit the take-home", 1),
            b.App("Margie's Travel", "QA Automation Engineer", Track.SDET, Channel.Djinni, 27,
                ApplicationStatus.NoResponse),
            b.App("Trey Research", "Test Automation Engineer", Track.SDET, Channel.LinkedIn, 11,
                ApplicationStatus.RecruiterScreen),
            b.App("Coho Winery", "SDET", Track.SDET, Channel.Platform, 8),
            b.App("Alpine Ski House", "QA Engineer", Track.SDET, Channel.LinkedIn, 20,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.Withdrawn)
                .Log(ActivityType.Note, "Withdrew: role was mostly manual testing", 10),
            b.App("Relecloud", "Automation Engineer", Track.SDET, Channel.CompanyWebsite, 23,
                ApplicationStatus.RecruiterScreen, ApplicationStatus.TechInterview, ApplicationStatus.Rejected),
            b.App("A. Datum Corporation", "SDET", Track.SDET, Channel.LinkedIn, 6),
            b.Interested("Humongous Insurance", "QA Lead", Track.SDET, Channel.LinkedIn),

            // Integration (2)
            b.App("VanArsdel", "Integration Developer", Track.Integration, Channel.Email, 10,
                ApplicationStatus.RecruiterScreen)
                .Contact(recruiterOmar),
            b.App("Fourth Coffee", "Integration Engineer", Track.Integration, Channel.LinkedIn, 5),
        };

        db.Contacts.AddRange(recruiterDana, recruiterOmar, referrerLena, managerRui);
        db.Applications.AddRange(applications.Select(a => a.Application));
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static Contact Contact(string name, ContactRole role, string company, string email) =>
        new(name, role) { Company = company, Email = email };

    /// <summary>Builds applications through the domain methods, so the data obeys the same rules as real use.</summary>
    private sealed class Builder(DateOnly today)
    {
        /// <summary>Applied <paramref name="appliedDaysAgo"/> days ago, then moved through <paramref name="history"/>.</summary>
        public DemoApplication App(string company, string role, Track track, Channel channel, int appliedDaysAgo, params ApplicationStatus[] history)
        {
            var applied = today.AddDays(-appliedDaysAgo);
            var application = new Application(company, role, track, channel, EmploymentType.Permanent);
            application.ChangeStatus(ApplicationStatus.Applied, applied);
            application.AddActivity(ActivityType.MessageSent, "Applied", applied);

            foreach (var status in history)
            {
                application.ChangeStatus(status, today);
            }

            return new DemoApplication(application, today);
        }

        public DemoApplication Interested(string company, string role, Track track, Channel channel) =>
            new(new Application(company, role, track, channel, EmploymentType.Permanent), today);
    }

    /// <summary>Fluent wrapper so each demo application reads as one statement; dates are relative to the seed's "today".</summary>
    private sealed class DemoApplication(Application application, DateOnly today)
    {
        public Application Application { get; } = application;

        public DemoApplication With(Action<Application> configure)
        {
            configure(Application);
            return this;
        }

        public DemoApplication Contact(Contact contact)
        {
            Application.Contacts.Add(contact);
            return this;
        }

        public DemoApplication Log(ActivityType type, string text, int daysAgo)
        {
            Application.AddActivity(type, text, today.AddDays(-daysAgo));
            return this;
        }

        public DemoApplication Next(string description, int dueInDays)
        {
            Application.SetNextAction(description, today.AddDays(dueInDays));
            return this;
        }
    }
}
