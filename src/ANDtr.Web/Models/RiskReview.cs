namespace ANDtr.Web.Models;

public class RiskReview
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;

    public int? OrganisationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? SharedAt { get; set; }

    public ApplicationUser? Owner { get; set; }

    public CustomerOrganisation? Organisation { get; set; }

    public ICollection<ReviewNotice> ReviewNotices { get; set; } = [];

    public ICollection<Comment> Comments { get; set; } = [];

    public ICollection<Notification> Notifications { get; set; } = [];

    public ICollection<ActivityLog> ActivityLogs { get; set; } = [];
}
