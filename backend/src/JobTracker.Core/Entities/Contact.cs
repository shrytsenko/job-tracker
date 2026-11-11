using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>A person in the job search: a recruiter, referrer, ex-colleague or hiring manager.</summary>
public class Contact
{
    /// <summary>Unique id, generated on creation.</summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>Full name. Required; change it through <see cref="UpdateDetails"/>.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>How they relate to the search. Required; change it through <see cref="UpdateDetails"/>.</summary>
    public ContactRole Role { get; private set; }

    /// <summary>
    /// The company they work for. Free text and independent of the applications' companies, because a recruiter
    /// often works for an agency rather than the hiring company.
    /// </summary>
    public string? Company { get; set; }

    /// <summary>Link to their LinkedIn profile.</summary>
    public string? LinkedInUrl { get; set; }

    /// <summary>Their email address.</summary>
    public string? Email { get; set; }

    /// <summary>Longest allowed <see cref="Notes"/>, enforced by the API and the database.</summary>
    public const int MaxNotesLength = 10000;

    /// <summary>Free-form notes (markdown).</summary>
    public string? Notes { get; set; }

    /// <summary>When the candidate last spoke to them. Set by the user.</summary>
    public DateOnly? LastContactOn { get; set; }

    /// <summary>The applications this person is linked to (many-to-many).</summary>
    public List<Application> Applications { get; private set; } = new();

    private Contact()
    {
    }

    /// <summary>Creates a contact with the required fields.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
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
