using System.Reflection;

namespace ChronoOverlay.Services;

public static class VersionService
{
    private static readonly Assembly ProductAssembly = typeof(VersionService).Assembly;

    public static string Current =>
        ProductAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? ProductAssembly.GetName().Version?.ToString(3)
        ?? throw new InvalidOperationException("ChronoOverlay assembly version metadata is unavailable.");
}
