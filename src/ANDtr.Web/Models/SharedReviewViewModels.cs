namespace ANDtr.Web.Models;

// Customer portal pages (A2-05): what a customer sees of reviews shared with their organisation.
public class SharedReviewsHomeViewModel
{
    public string? OrganisationName { get; set; }

    public List<SharedReviewListItem> Reviews { get; set; } = [];
}

public class SharedReviewListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SharedBy { get; set; } = string.Empty;

    public DateTime SharedAt { get; set; }

    public int NoticeCount { get; set; }

    public int CommentCount { get; set; }
}

public class SharedReviewDetails
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OrganisationName { get; set; } = string.Empty;

    public string SharedBy { get; set; } = string.Empty;

    public DateTime SharedAt { get; set; }

    public List<SharedNoticeItem> Notices { get; set; } = [];

    // Internal comments are never included.
    public List<SharedCommentItem> Comments { get; set; } = [];
}

public class SharedNoticeItem
{
    public string RecallNumber { get; set; } = string.Empty;

    public string ProductCodeName { get; set; } = string.Empty;

    public string ProductDescription { get; set; } = string.Empty;

    public string FirmName { get; set; } = string.Empty;

    public int RecallClass { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string WebAddress { get; set; } = string.Empty;
}

public class SharedCommentItem
{
    public string Author { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
