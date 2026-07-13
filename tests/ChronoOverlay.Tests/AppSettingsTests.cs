using ChronoOverlay.Models;

namespace ChronoOverlay.Tests;

public sealed class AppSettingsTests
{
    [Fact]
    public void NormalizeClampsEveryNumericSetting()
    {
        AppSettings settings = new()
        {
            TimeFontSize = 999,
            DateFontSize = -5,
            BackgroundOpacity = 3,
            Hue = -30,
            SavedDpi = 2,
            TextColor = "not-a-color",
        };

        settings.Normalize();

        Assert.Equal(128, settings.TimeFontSize);
        Assert.Equal(16, settings.DateFontSize);
        Assert.Equal(1, settings.BackgroundOpacity);
        Assert.Equal(330, settings.Hue);
        Assert.Equal(96, settings.SavedDpi);
        Assert.Equal("#FFFFFF", settings.TextColor);
    }

    [Fact]
    public void NormalizePreservesNegativeMultiMonitorCoordinates()
    {
        AppSettings settings = new() { WindowX = -1400, WindowY = -20 };

        settings.Normalize();

        Assert.Equal(-1400, settings.WindowX);
        Assert.Equal(-20, settings.WindowY);
    }
}
