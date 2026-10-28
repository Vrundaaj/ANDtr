namespace ANDtr.Web.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int RiskReviewId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public ApplicationUser? User { get; set; }

    public RiskReview? RiskReview { get; set; }
}
