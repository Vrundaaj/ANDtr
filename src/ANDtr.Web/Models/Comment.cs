namespace ANDtr.Web.Models;

public class Comment
{
    public int Id { get; set; }

    public int RiskReviewId { get; set; }

    public int? ReviewNoticeId { get; set; }

    public string AuthorId { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }

    public RiskReview? RiskReview { get; set; }

    public ReviewNotice? ReviewNotice { get; set; }

    public ApplicationUser? Author { get; set; }
}
