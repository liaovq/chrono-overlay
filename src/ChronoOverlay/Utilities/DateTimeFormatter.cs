using System.Globalization;

namespace ChronoOverlay.Utilities;

public static class DateTimeFormatter
{
    private static readonly string[] ChineseWeekdays =
    [
        "星期日",
        "星期一",
        "星期二",
        "星期三",
        "星期四",
        "星期五",
        "星期六",
    ];

    public static string FormatTime(DateTime value) => value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

    public static string FormatDate(DateTime value) =>
        $"{value.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)} {ChineseWeekdays[(int)value.DayOfWeek]}";
}
