namespace ChronoOverlay.Models;

public static class ClockFontIds
{
    public const string SystemMono = "system-mono";
    public const string JetBrainsMono = "jetbrains-mono";
    public const string IbmPlexMono = "ibm-plex-mono";
    public const string SpaceMono = "space-mono";
    public const string MajorMonoDisplay = "major-mono-display";
    public const string Vt323 = "vt323";

    public static readonly IReadOnlyList<string> All =
    [
        SystemMono,
        JetBrainsMono,
        IbmPlexMono,
        SpaceMono,
        MajorMonoDisplay,
        Vt323,
    ];

    public static string Normalize(string? value) =>
        All.FirstOrDefault(id => string.Equals(id, value, StringComparison.OrdinalIgnoreCase)) ?? SystemMono;
}
