using ANDtr.Web.Models;

namespace ANDtr.Tests;

public sealed class DisplayTextTests
{
    [Theory]
    [InlineData("Ananya Iyer", "AI")]
    [InlineData("priya", "P")]
    [InlineData("rahul@andtr.com", "RA")]
    [InlineData("", "?")]
    [InlineData(null, "?")]
    public void Initials_ReturnsUpToTwoCapitalLetters(string? name, string expected)
    {
        Assert.Equal(expected, DisplayText.Initials(name));
    }

    [Fact]
    public void TimeAgo_FormatsElapsedTime()
    {
        var now = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal("Never", DisplayText.TimeAgo(null, now));
        Assert.Equal("Just now", DisplayText.TimeAgo(now.AddSeconds(-20), now));
        Assert.Equal("5 min ago", DisplayText.TimeAgo(now.AddMinutes(-5), now));
        Assert.Equal("3 h ago", DisplayText.TimeAgo(now.AddHours(-3), now));
        Assert.Equal("1 day ago", DisplayText.TimeAgo(now.AddHours(-30), now));
        Assert.Equal("4 days ago", DisplayText.TimeAgo(now.AddDays(-4), now));
    }
}
