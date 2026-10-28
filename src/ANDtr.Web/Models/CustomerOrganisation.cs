namespace ANDtr.Web.Models;

public class CustomerOrganisation
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = [];

    public ICollection<RiskReview> RiskReviews { get; set; } = [];
}
