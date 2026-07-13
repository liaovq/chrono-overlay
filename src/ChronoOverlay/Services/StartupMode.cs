namespace ChronoOverlay.Services;

public enum StartupMode
{
    Interactive,
    AutoStart,
}

public static class StartupModeParser
{
    private const string AutoStartArgument = "--autostart";

    public static StartupMode Parse(IEnumerable<string> arguments) =>
        arguments.Any(argument => string.Equals(argument, AutoStartArgument, StringComparison.OrdinalIgnoreCase))
            ? StartupMode.AutoStart
            : StartupMode.Interactive;
}
