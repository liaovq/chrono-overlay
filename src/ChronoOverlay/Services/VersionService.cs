using System.Reflection;

namespace ChronoOverlay.Services;

public static class VersionService
{
    public static string Current =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
        ?? "0.1.0";
}
