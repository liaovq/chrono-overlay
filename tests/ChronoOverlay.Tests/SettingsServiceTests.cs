using System.Text.Json;
using ChronoOverlay.Models;
using ChronoOverlay.Services;

namespace ChronoOverlay.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ChronoOverlay.Tests-{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoadRoundTripsSettingsAtomically()
    {
        using SettingsService service = new(_directory);
        AppSettings expected = new()
        {
            WindowX = -800.5,
            WindowY = 42.25,
            WindowPhysicalX = -1200,
            WindowPhysicalY = 80,
            WindowPhysicalWidth = 640,
            WindowPhysicalHeight = 420,
            ClockAnchorOffsetX = 1510,
            ClockAnchorOffsetY = 44,
            MonitorDeviceName = @"\\.\DISPLAY2",
            SavedDpi = 144,
            TimeFontSize = 88,
            DateFontSize = 28,
            ClockFontId = ClockFontIds.JetBrainsMono,
            TextColor = "#12A4FF",
            ColorMode = ColorMode.Hue,
            Hue = 203,
            BackgroundOpacity = 0.35,
            IsLocked = true,
            AutoStartEnabled = true,
        };

        service.SaveImmediately(expected);
        AppSettings actual = service.Load();

        Assert.Equal(expected.WindowX, actual.WindowX);
        Assert.Equal(expected.WindowY, actual.WindowY);
        Assert.Equal(-1200, actual.WindowPhysicalX);
        Assert.Equal(1510, actual.ClockAnchorOffsetX);
        Assert.Equal(@"\\.\DISPLAY2", actual.MonitorDeviceName);
        Assert.Equal(144, actual.SavedDpi);
        Assert.Equal(88, actual.TimeFontSize);
        Assert.Equal(ClockFontIds.JetBrainsMono, actual.ClockFontId);
        Assert.Equal("#12A4FF", actual.TextColor);
        Assert.True(actual.IsLocked);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void MissingFieldsUseModelDefaults()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "config.json"), "{}");
        using SettingsService service = new(_directory);

        AppSettings settings = service.Load();

        Assert.Equal(72, settings.TimeFontSize);
        Assert.Equal(32, settings.DateFontSize);
        Assert.Equal(0.60, settings.BackgroundOpacity);
        Assert.Equal(ClockFontIds.SystemMono, settings.ClockFontId);
        Assert.Equal(VersionService.Current, settings.AppVersion);
    }

    [Fact]
    public void SchemaTwoConfigurationMigratesToDefaultClockFont()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "config.json"), "{\"schemaVersion\":2,\"timeFontSize\":80}");
        using SettingsService service = new(_directory);

        AppSettings settings = service.Load();

        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Equal(ClockFontIds.SystemMono, settings.ClockFontId);
        Assert.Equal(80, settings.TimeFontSize);
    }

    [Fact]
    public void MissingAppVersionIsReplacedWithRuntimeVersion()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "config.json"), "{\"schemaVersion\":1}");
        using SettingsService service = new(_directory);

        AppSettings settings = service.Load();

        Assert.Equal(VersionService.Current, settings.AppVersion);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void OldAppVersionIsReplacedWithRuntimeVersion()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(
            Path.Combine(_directory, "config.json"),
            "{\"schemaVersion\":1,\"appVersion\":\"0.0.7\"}");
        using SettingsService service = new(_directory);

        AppSettings settings = service.Load();

        Assert.Equal(VersionService.Current, settings.AppVersion);
    }

    [Fact]
    public void CorruptJsonIsBackedUpAndDefaultsAreReturned()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "config.json"), "{ definitely invalid");
        using SettingsService service = new(_directory);

        AppSettings settings = service.Load();

        Assert.Equal(72, settings.TimeFontSize);
        Assert.False(File.Exists(Path.Combine(_directory, "config.json")));
        Assert.Single(Directory.GetFiles(_directory, "config.corrupt-*.json"));
    }

    [Fact]
    public void SerializationUsesCamelCaseAndStringEnums()
    {
        string json = SettingsService.Serialize(new AppSettings { ColorMode = ColorMode.Black });
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.Equal("Black", document.RootElement.GetProperty("colorMode").GetString());
        Assert.True(document.RootElement.TryGetProperty("backgroundOpacity", out _));
    }

    [Fact]
    public void SerializationAlwaysWritesRuntimeAppVersion()
    {
        AppSettings settings = new() { AppVersion = "0.0.1" };

        string json = SettingsService.Serialize(settings);
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.Equal(VersionService.Current, document.RootElement.GetProperty("appVersion").GetString());
        Assert.Equal(VersionService.Current, settings.AppVersion);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
