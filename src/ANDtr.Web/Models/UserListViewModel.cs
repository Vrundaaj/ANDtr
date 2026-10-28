namespace ANDtr.Web.Models;

public class UserListItem
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Role { get; set; }

    public string? Organisation { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    // Locked by Identity after too many wrong passwords.
    public bool IsLocked => LockoutEnd > DateTimeOffset.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}

// Users list page: the current filters and sort plus one page of results (A1, A3).
public class UserListViewModel
{
    public string? Search { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    // Shows only deleted users; otherwise deleted users are hidden.
    public bool Deleted { get; set; }

    // The Status dropdown ("active", "inactive" or "deleted"), mapped onto IsActive and Deleted.
    public string? Status
    {
        get => Deleted ? "deleted" : IsActive switch { true => "active", false => "inactive", null => null };
        set
        {
            Deleted = value == "deleted";
            IsActive = value switch { "active" => true, "inactive" => false, _ => null };
        }
    }

    public int? OrganisationId { get; set; }

    public string? OrganisationName { get; set; }

    // Column key from UserSort; empty means name.
    public string? Sort { get; set; }

    public bool Desc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public List<UserListItem> Users { get; set; } = [];
}

public static class UserSort
{
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string Organisation = "organisation";
    public const string Status = "status";
    public const string LastLogin = "lastlogin";
}
