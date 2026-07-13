using ChronoOverlay.Utilities;

namespace ChronoOverlay.Tests;

public sealed class DateTimeFormatterTests
{
    [Fact]
    public void TimeUsesTwentyFourHourFormatWithSeconds()
    {
        DateTime value = new(2026, 7, 13, 4, 5, 6);

        Assert.Equal("04:05:06", DateTimeFormatter.FormatTime(value));
    }

    [Theory]
    [InlineData(2026, 7, 12, "2026.07.12 星期日")]
    [InlineData(2026, 7, 13, "2026.07.13 星期一")]
    [InlineData(2026, 7, 18, "2026.07.18 星期六")]
    public void DateUsesInvariantChineseWeekday(int year, int month, int day, string expected)
    {
        Assert.Equal(expected, DateTimeFormatter.FormatDate(new DateTime(year, month, day)));
    }
}
