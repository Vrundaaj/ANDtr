namespace ANDtr.Web.Models;

public class Notice
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductCodeName { get; set; } = string.Empty;

    public string RegulationNumber { get; set; } = string.Empty;

    public string WebAddress { get; set; } = string.Empty;

    public string RecallNumber { get; set; } = string.Empty;

    public string ProductDescription { get; set; } = string.Empty;

    public string? TradeName { get; set; }

    public int RecallClass { get; set; }

    public DateOnly CenterClassificationDate { get; set; }

    public DateOnly? PostedInternetDate { get; set; }

    public DateOnly? TerminationDate { get; set; }

    public string? FeiNumber { get; set; }

    public string FirmName { get; set; } = string.Empty;

    public string ManufacturerRecallReason { get; set; } = string.Empty;

    public ICollection<ReviewNotice> ReviewNotices { get; set; } = [];
}
