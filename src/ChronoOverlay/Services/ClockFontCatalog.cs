using ChronoOverlay.Models;
using MediaFontFamily = System.Windows.Media.FontFamily;

namespace ChronoOverlay.Services;

public sealed record ClockFontOption(string Id, string DisplayName, string Description, MediaFontFamily FontFamily);

public static class ClockFontCatalog
{
    private static readonly Uri ApplicationBaseUri = CreateApplicationBaseUri();

    public static IReadOnlyList<ClockFontOption> Options { get; } =
    [
        new(
            ClockFontIds.SystemMono,
            "系统等宽",
            "系统默认",
            new MediaFontFamily("Cascadia Mono, Consolas")),
        new(
            ClockFontIds.JetBrainsMono,
            "JetBrains Mono",
            "清晰紧凑",
            Embedded("JetBrains Mono")),
        new(
            ClockFontIds.IbmPlexMono,
            "IBM Plex Mono",
            "理性机械",
            Embedded("IBM Plex Mono")),
        new(
            ClockFontIds.SpaceMono,
            "Space Mono",
            "复古醒目",
            Embedded("Space Mono")),
        new(
            ClockFontIds.MajorMonoDisplay,
            "Major Mono Display",
            "几何实验",
            Embedded("Major Mono Display")),
        new(
            ClockFontIds.Vt323,
            "VT323",
            "像素终端",
            Embedded("VT323")),
    ];

    public static ClockFontOption Resolve(string? id)
    {
        string normalized = ClockFontIds.Normalize(id);
        return Options.First(option => option.Id == normalized);
    }

    private static MediaFontFamily Embedded(string familyName) =>
        new(ApplicationBaseUri, $"./Assets/Fonts/#{familyName}");

    private static Uri CreateApplicationBaseUri()
    {
        // PackUriHelper registers WPF's pack:// URI parser. Unit-test hosts do not create an
        // Application instance first, so force that registration before constructing the URI.
        _ = System.IO.Packaging.PackUriHelper.CreatePartUri(new Uri("/bootstrap", UriKind.Relative));
        return new Uri("pack://application:,,,/", UriKind.Absolute);
    }
}
