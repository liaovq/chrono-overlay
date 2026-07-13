using System.Globalization;
using MediaColor = System.Windows.Media.Color;

namespace ChronoOverlay.Utilities;

public static class ColorUtilities
{
    public static double NormalizeHue(double hue)
    {
        double normalized = hue % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    public static MediaColor FromHue(double hue, double saturation = 0.88, double lightness = 0.60)
    {
        hue = NormalizeHue(hue);
        saturation = Math.Clamp(saturation, 0, 1);
        lightness = Math.Clamp(lightness, 0, 1);

        double chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        double section = hue / 60;
        double x = chroma * (1 - Math.Abs((section % 2) - 1));
        (double r, double g, double b) = section switch
        {
            < 1 => (chroma, x, 0d),
            < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma),
            _ => (chroma, 0d, x),
        };
        double m = lightness - (chroma / 2);

        return MediaColor.FromRgb(
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }

    public static string ToHex(MediaColor color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static string NormalizeHex(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string candidate = value.Trim().TrimStart('#');
        if (candidate.Length == 6 && int.TryParse(candidate, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return $"#{candidate.ToUpperInvariant()}";
        }

        return fallback;
    }
}
