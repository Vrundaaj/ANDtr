namespace ANDtr.Web.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public int? RiskReviewId { get; set; }

    public DateTime CreatedAt { get; set; }

    public ApplicationUser? User { get; set; }

    public RiskReview? RiskReview { get; set; }
}
