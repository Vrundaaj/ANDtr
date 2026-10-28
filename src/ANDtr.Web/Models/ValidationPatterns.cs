namespace ANDtr.Web.Models;

public static class ValidationPatterns
{
    // Stricter than [EmailAddress]: the domain must contain a dot, so "ananya@sanjivani" is refused.
    public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

    public const string EmailMessage = "Please enter a valid email address.";
}
