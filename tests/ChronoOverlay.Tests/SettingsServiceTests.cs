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
            TimeFontSize = 88,
            DateFontSize = 28,
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
        Assert.Equal(88, actual.TimeFontSize);
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

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
