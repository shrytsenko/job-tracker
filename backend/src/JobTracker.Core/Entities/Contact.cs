using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

public class Contact
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;
    public ContactRole Role { get; private set; }
    public string? Company { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public DateOnly? LastContactOn { get; set; }

    public List<Application> Applications { get; private set; } = new();

    private Contact()
    {
    }

    public Contact(string name, ContactRole role)
    {
        UpdateDetails(name, role);
    }

    /// <summary>Sets the required fields, enforcing the same rules as the constructor.</summary>
    public void UpdateDetails(string name, ContactRole role)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Role = role;
    }
}
