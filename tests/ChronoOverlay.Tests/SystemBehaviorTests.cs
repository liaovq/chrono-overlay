using System.Drawing;
using ChronoOverlay.Services;

namespace ChronoOverlay.Tests;

public sealed class SystemBehaviorTests
{
    [Fact]
    public void AutoStartCommandQuotesPathsAndMarksStartup()
    {
        string command = AutoStartService.BuildCommand(@"C:\Program Files\ChronoOverlay\ChronoOverlay.exe");

        Assert.Equal("\"C:\\Program Files\\ChronoOverlay\\ChronoOverlay.exe\" --autostart", command);
    }

    [Fact]
    public void PlacementAcceptsWindowWithMinimumVisibleArea()
    {
        Rectangle window = new(980, 200, 100, 100);
        Rectangle[] screens = [new Rectangle(0, 0, 1000, 800)];

        Assert.False(DisplayPlacementService.IsVisibleOnAnyScreen(window, screens, 32));
        Assert.True(DisplayPlacementService.IsVisibleOnAnyScreen(window, screens, 20));
    }

    [Fact]
    public void PlacementSupportsNegativeDisplayCoordinates()
    {
        Rectangle window = new(-1200, 100, 400, 220);
        Rectangle[] screens = [new Rectangle(-1920, 0, 1920, 1080), new Rectangle(0, 0, 1920, 1080)];

        Assert.True(DisplayPlacementService.IsVisibleOnAnyScreen(window, screens, 32));
    }
}
