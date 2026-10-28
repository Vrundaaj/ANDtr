namespace ANDtr.Web.Models;

// Small formatting helpers shared by the views and the exported reports.
public static class DisplayText
{
    // "Ananya Iyer" -> "AI"; falls back to the first letter of a single word or email.
    public static string Initials(string? name)
    {
        var words = (name ?? string.Empty).Split([' ', '@', '.'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length switch
        {
            0 => "?",
            1 => words[0][..1].ToUpperInvariant(),
            _ => $"{words[0][0]}{words[1][0]}".ToUpperInvariant(),
        };
    }

    // "Just now", "5 min ago", "3 days ago", "12/10/2026" - or "Never".
    public static string TimeAgo(DateTime? utc, DateTime? now = null)
    {
        if (utc is null)
        {
            return "Never";
        }

        var elapsed = (now ?? DateTime.UtcNow) - utc.Value;
        return elapsed.TotalMinutes switch
        {
            < 1 => "Just now",
            < 60 => $"{(int)elapsed.TotalMinutes} min ago",
            < 60 * 24 => $"{(int)elapsed.TotalHours} h ago",
            < 60 * 24 * 2 => "1 day ago",
            < 60 * 24 * 30 => $"{(int)elapsed.TotalDays} days ago",
            _ => utc.Value.ToLocalTime().ToString("dd/MM/yyyy"),
        };
    }
}
