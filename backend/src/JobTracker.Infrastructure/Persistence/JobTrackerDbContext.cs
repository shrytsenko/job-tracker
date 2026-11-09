using JobTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Persistence;

public class JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options) : DbContext(options)
{
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>(entity =>
        {
            // Ids are generated in the domain (Guid.NewGuid), not by the database. Telling EF this
            // means new entities added through a navigation (e.g. Activities) are INSERTed, not UPDATEd.
            entity.Property(a => a.Id).ValueGeneratedNever();

            entity.Property(a => a.Company).HasMaxLength(200);
            entity.Property(a => a.RoleTitle).HasMaxLength(200);
            entity.Property(a => a.JobUrl).HasMaxLength(2000);
            entity.Property(a => a.RemoteRegion).HasMaxLength(100);
            entity.Property(a => a.HybridCity).HasMaxLength(100);
            entity.Property(a => a.CompensationCurrency).HasConversion<string>().HasMaxLength(3);
            entity.Property(a => a.NextActionDescription).HasMaxLength(500);

            // Enums stored as text: readable in the DB file and safe if enum members are reordered.
            entity.Property(a => a.Track).HasConversion<string>().HasMaxLength(30);
            entity.Property(a => a.Channel).HasConversion<string>().HasMaxLength(30);
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(a => a.EmploymentType).HasConversion<string>().HasMaxLength(30);
            entity.Property(a => a.CompensationPeriod).HasConversion<string>().HasMaxLength(30);

            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.Track);

            entity.HasMany(a => a.Activities)
                .WithOne()
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(a => a.Contacts)
                .WithMany(c => c.Applications)
                .UsingEntity(join => join.ToTable("ApplicationContacts"));
        });

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.Name).HasMaxLength(200);
            entity.Property(c => c.Company).HasMaxLength(200);
            entity.Property(c => c.LinkedInUrl).HasMaxLength(2000);
            entity.Property(c => c.Email).HasMaxLength(320);
            entity.Property(c => c.Role).HasConversion<string>().HasMaxLength(30);
        });

        modelBuilder.Entity<Activity>(entity =>
        {
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.Text).HasMaxLength(2000);
            entity.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        });
    }
}
