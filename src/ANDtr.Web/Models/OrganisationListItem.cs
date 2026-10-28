namespace ANDtr.Web.Models;

public class OrganisationListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public int UserCount { get; set; }

    public int SharedReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
