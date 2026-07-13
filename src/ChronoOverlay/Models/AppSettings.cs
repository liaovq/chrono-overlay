using ChronoOverlay.Utilities;

namespace ChronoOverlay.Models;

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 3;
    public const double MinTimeFontSize = 32;
    public const double MaxTimeFontSize = 128;
    public const double MinDateFontSize = 16;
    public const double MaxDateFontSize = 64;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string AppVersion { get; set; } = string.Empty;

    public double? WindowX { get; set; }

    public double? WindowY { get; set; }

    public int? WindowPhysicalX { get; set; }

    public int? WindowPhysicalY { get; set; }

    public int? WindowPhysicalWidth { get; set; }

    public int? WindowPhysicalHeight { get; set; }

    public int? ClockAnchorOffsetX { get; set; }

    public int? ClockAnchorOffsetY { get; set; }

    public string? MonitorDeviceName { get; set; }

    public double SavedDpi { get; set; } = 96;

    public double TimeFontSize { get; set; } = 72;

    public double DateFontSize { get; set; } = 32;

    public string ClockFontId { get; set; } = ClockFontIds.SystemMono;

    public string TextColor { get; set; } = "#FFFFFF";

    public ColorMode ColorMode { get; set; } = ColorMode.White;

    public double Hue { get; set; } = 205;

    public double BackgroundOpacity { get; set; } = 0.60;

    public bool IsLocked { get; set; }

    public bool AutoStartEnabled { get; set; }

    public AppSettings Normalize()
    {
        SchemaVersion = CurrentSchemaVersion;
        TimeFontSize = Math.Clamp(TimeFontSize, MinTimeFontSize, MaxTimeFontSize);
        DateFontSize = Math.Clamp(DateFontSize, MinDateFontSize, MaxDateFontSize);
        ClockFontId = ClockFontIds.Normalize(ClockFontId);
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0, 1);
        Hue = ColorUtilities.NormalizeHue(Hue);
        SavedDpi = SavedDpi is >= 48 and <= 768 ? SavedDpi : 96;
        TextColor = ColorUtilities.NormalizeHex(TextColor, "#FFFFFF");

        if (!Enum.IsDefined(ColorMode))
        {
            ColorMode = ColorMode.White;
        }

        return this;
    }
}
