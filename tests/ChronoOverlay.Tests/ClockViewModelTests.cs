using ChronoOverlay.Models;
using ChronoOverlay.Services;
using ChronoOverlay.ViewModels;

namespace ChronoOverlay.Tests;

public sealed class ClockViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ChronoOverlay.ViewModelTests-{Guid.NewGuid():N}");

    [Fact]
    public void BlackWhiteAndHueModesStaySynchronized()
    {
        AppSettings settings = new();
        using SettingsService settingsService = new(_directory);
        using ClockViewModel viewModel = new(settings, new ClockService(), settingsService);

        viewModel.SetBlack();
        Assert.Equal(ColorMode.Black, settings.ColorMode);
        Assert.Equal("#000000", settings.TextColor);

        viewModel.SetWhite();
        Assert.Equal(ColorMode.White, settings.ColorMode);
        Assert.Equal("#FFFFFF", settings.TextColor);

        viewModel.Hue = 120;
        Assert.Equal(ColorMode.Hue, settings.ColorMode);
        Assert.Equal(120, settings.Hue);
        Assert.NotEqual("#000000", settings.TextColor);
        Assert.NotEqual("#FFFFFF", settings.TextColor);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
