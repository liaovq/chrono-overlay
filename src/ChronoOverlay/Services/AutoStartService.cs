using Microsoft.Win32;

namespace ChronoOverlay.Services;

public sealed class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ChronoOverlay";

    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (enabled)
        {
            key.SetValue(ValueName, BuildCommand(Environment.ProcessPath ?? throw new InvalidOperationException("无法获取程序路径。")));
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }
    }

    public void RepairIfEnabled()
    {
        if (IsEnabled())
        {
            SetEnabled(true);
        }
    }

    public static string BuildCommand(string executablePath) => $"\"{executablePath}\" --autostart";
}
