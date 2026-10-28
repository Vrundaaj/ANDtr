using Microsoft.AspNetCore.Identity;

namespace ANDtr.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public int? OrganisationId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    // Set when an admin deletes the account: it is hidden from the users list but kept, with its history.
    public DateTime? DeletedAt { get; set; }

    public CustomerOrganisation? Organisation { get; set; }
}
