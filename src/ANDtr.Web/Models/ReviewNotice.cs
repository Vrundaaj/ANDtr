namespace ANDtr.Web.Models;

public class ReviewNotice
{
    public int Id { get; set; }

    public int RiskReviewId { get; set; }

    public int NoticeId { get; set; }

    public DateTime AddedAt { get; set; }

    public RiskReview? RiskReview { get; set; }

    public Notice? Notice { get; set; }

    public ICollection<Comment> Comments { get; set; } = [];
}
