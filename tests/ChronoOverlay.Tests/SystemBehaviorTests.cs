using System.Drawing;
using System.Threading;
using ChronoOverlay.Models;
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

    [Theory]
    [InlineData(new string[0], StartupMode.Interactive)]
    [InlineData(new[] { "--autostart" }, StartupMode.AutoStart)]
    [InlineData(new[] { "--AUTOSTART" }, StartupMode.AutoStart)]
    [InlineData(new[] { "--other", "--autostart" }, StartupMode.AutoStart)]
    public void StartupModeParserRecognizesAutoStart(string[] arguments, StartupMode expected)
    {
        Assert.Equal(expected, StartupModeParser.Parse(arguments));
    }

    [Fact]
    public void WakeSignalSentBeforeListenerRegistrationIsConsumed()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string mutexName = $@"Local\ChronoOverlay.Tests.{suffix}.Mutex";
        string eventName = $@"Local\ChronoOverlay.Tests.{suffix}.Wake";
        using SingleInstanceService primary = new(mutexName, eventName);
        using SingleInstanceService secondary = new(mutexName, eventName);
        using ManualResetEventSlim received = new(false);

        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);

        secondary.SignalPrimary();
        primary.StartListening(received.Set);

        Assert.True(received.Wait(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public void SingleInstanceDisposeReleasesNamedPrimitives()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string mutexName = $@"Local\ChronoOverlay.Tests.{suffix}.Mutex";
        string eventName = $@"Local\ChronoOverlay.Tests.{suffix}.Wake";
        SingleInstanceService first = new(mutexName, eventName);
        Assert.True(first.IsPrimary);

        first.Dispose();

        using SingleInstanceService replacement = new(mutexName, eventName);
        Assert.True(replacement.IsPrimary);
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

    [Fact]
    public void PlacementRestoresAnchorOnSavedMixedDpiMonitor()
    {
        AppSettings settings = new()
        {
            MonitorDeviceName = @"\\.\DISPLAY2",
            SavedDpi = 96,
            ClockAnchorOffsetX = 640,
            ClockAnchorOffsetY = 120,
        };
        MonitorWorkArea[] monitors =
        [
            new(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1040), 96, true),
            new(@"\\.\DISPLAY2", new Rectangle(-2560, 0, 2560, 1400), 144, false),
        ];

        PlacementResolution result = DisplayPlacementMath.ResolveWindowPosition(
            settings,
            monitors,
            new Size(500, 300),
            new Point(480, 20));

        Assert.Equal(@"\\.\DISPLAY2", result.Monitor.DeviceName);
        Assert.Equal(new Point(-2080, 160), result.WindowTopLeft);
        Assert.True(result.RequiresPersistence);
    }

    [Fact]
    public void PlacementFallsBackToPrimaryWhenSavedMonitorIsMissing()
    {
        AppSettings settings = new()
        {
            MonitorDeviceName = @"\\.\DISPLAY9",
            SavedDpi = 144,
            ClockAnchorOffsetX = 800,
            ClockAnchorOffsetY = 150,
        };
        MonitorWorkArea[] monitors =
        [
            new(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1040), 96, true),
        ];

        PlacementResolution result = DisplayPlacementMath.ResolveWindowPosition(
            settings,
            monitors,
            new Size(400, 240),
            new Point(380, 16));

        Assert.Equal(new Point(1516, 8), result.WindowTopLeft);
        Assert.True(result.RequiresPersistence);
        Assert.Equal(@"\\.\DISPLAY1", result.Monitor.DeviceName);
    }

    [Fact]
    public void PlacementStaysOnSavedMonitorAfterResolutionShrinks()
    {
        AppSettings settings = new()
        {
            MonitorDeviceName = @"\\.\DISPLAY2",
            SavedDpi = 96,
            ClockAnchorOffsetX = 3800,
            ClockAnchorOffsetY = 100,
        };
        MonitorWorkArea[] monitors =
        [
            new(@"\\.\DISPLAY2", new Rectangle(-1920, 0, 1920, 1040), 96, false),
            new(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1040), 96, true),
        ];

        PlacementResolution result = DisplayPlacementMath.ResolveWindowPosition(
            settings,
            monitors,
            new Size(500, 300),
            new Point(480, 20));

        Assert.Equal(@"\\.\DISPLAY2", result.Monitor.DeviceName);
        Assert.Equal(new Point(-504, 4), result.WindowTopLeft);
        Assert.True(result.RequiresPersistence);
    }

    [Fact]
    public void PlacementMigratesSchemaOneDipCoordinatesUsingSavedDpi()
    {
        AppSettings settings = new()
        {
            SchemaVersion = 1,
            MonitorDeviceName = @"\\.\DISPLAY2",
            SavedDpi = 144,
            WindowX = -1000,
            WindowY = 100,
        };
        MonitorWorkArea[] monitors =
        [
            new(@"\\.\DISPLAY1", new Rectangle(0, 0, 1920, 1040), 96, true),
            new(@"\\.\DISPLAY2", new Rectangle(-1920, 0, 1920, 1080), 144, false),
        ];

        PlacementResolution result = DisplayPlacementMath.ResolveWindowPosition(
            settings,
            monitors,
            new Size(500, 300),
            new Point(480, 20));

        Assert.Equal(new Point(-1500, 150), result.WindowTopLeft);
        Assert.True(result.RequiresPersistence);
    }

    [Theory]
    [InlineData(100, 96, 144, 150)]
    [InlineData(-200, 144, 96, -133)]
    [InlineData(80, 2, 192, 160)]
    public void PlacementScalesOffsetsBetweenDpiValues(int value, double savedDpi, uint targetDpi, int expected)
    {
        Assert.Equal(expected, DisplayPlacementMath.ScaleForDpi(value, savedDpi, targetDpi));
    }

    [Theory]
    [InlineData(32, 340)]
    [InlineData(72, 410)]
    [InlineData(128, 520)]
    public void LockTransitionPreservesClockTopRightAnchor(int fontSize, int expandedClockAnchorX)
    {
        Point desiredAnchor = new(1800, 120);
        Point collapsedWindowTopLeft = new(1300, 100);
        Point currentAnchor = new(expandedClockAnchorX + fontSize, 120);

        Point compensated = DisplayPlacementMath.PreserveAnchor(
            collapsedWindowTopLeft,
            currentAnchor,
            desiredAnchor);

        Assert.Equal(desiredAnchor.X, currentAnchor.X + compensated.X - collapsedWindowTopLeft.X);
        Assert.Equal(desiredAnchor.Y, currentAnchor.Y + compensated.Y - collapsedWindowTopLeft.Y);
    }

    [Fact]
    public void ControlPanelStaysBelowWhenItFitsInWorkArea()
    {
        Rectangle workArea = new(0, 0, 1920, 1040);
        Rectangle clock = new(1400, 200, 480, 160);

        ControlPanelPlacement placement = ControlPanelPlacementMath.Resolve(workArea, clock, 280, 6);

        Assert.Equal(ControlPanelPlacement.Below, placement);
    }

    [Fact]
    public void ControlPanelFlipsAboveWhenBottomWouldBeClipped()
    {
        Rectangle workArea = new(0, 0, 1920, 1040);
        Rectangle clock = new(1400, 820, 480, 160);

        ControlPanelPlacement placement = ControlPanelPlacementMath.Resolve(workArea, clock, 280, 6);

        Assert.Equal(ControlPanelPlacement.Above, placement);
    }

    [Fact]
    public void ControlPanelSupportsNegativeMonitorCoordinates()
    {
        Rectangle workArea = new(-2560, -200, 2560, 1400);
        Rectangle clock = new(-700, 870, 620, 140);

        ControlPanelPlacement placement = ControlPanelPlacementMath.Resolve(workArea, clock, 300, 8);

        Assert.Equal(ControlPanelPlacement.Above, placement);
    }

    [Fact]
    public void ControlPanelUsesSideWithMoreSpaceWhenNeitherSideFits()
    {
        Rectangle workArea = new(0, 0, 800, 500);
        Rectangle clock = new(200, 300, 400, 120);

        ControlPanelPlacement placement = ControlPanelPlacementMath.Resolve(workArea, clock, 400, 6);

        Assert.Equal(ControlPanelPlacement.Above, placement);
    }

    [Theory]
    [InlineData(ControlPanelPlacement.Below, 0, 20)]
    [InlineData(ControlPanelPlacement.Above, 20, 0)]
    public void ControlPanelShadowInsetMovesToOuterWindowEdge(
        ControlPanelPlacement placement,
        double expectedTop,
        double expectedBottom)
    {
        System.Windows.Thickness margin = ControlPanelShadowLayout.GetChromeMargin(placement);

        Assert.Equal(12, margin.Left);
        Assert.Equal(expectedTop, margin.Top);
        Assert.Equal(12, margin.Right);
        Assert.Equal(expectedBottom, margin.Bottom);
        Assert.Equal(20, margin.Top + margin.Bottom);
    }

    [Fact]
    public void AuroraControlDockKeepsSelectedReferenceProportions()
    {
        Assert.Equal(420, ControlPanelShadowLayout.VisiblePanelWidth);
        Assert.Equal(166, ControlPanelShadowLayout.VisiblePanelHeight);
        Assert.Equal(444, ControlPanelShadowLayout.OuterWidth);
        Assert.Equal(
            ControlPanelShadowLayout.VisiblePanelWidth + (ControlPanelShadowLayout.HorizontalInset * 2),
            ControlPanelShadowLayout.OuterWidth);
    }

    [Theory]
    [InlineData(32, 1480, 410)]
    [InlineData(72, 1540, 470)]
    [InlineData(128, 1620, 560)]
    public void FontSizeChangesKeepControlPanelTopRightFixed(
        int fontSize,
        int resizedPanelLeft,
        int resizedPanelTop)
    {
        Point originalWindowTopLeft = new(1000, 200);
        Rectangle resizedPanel = new(resizedPanelLeft, resizedPanelTop, 420, 166);
        Point desiredPanelTopRight = new(1900, 400);

        Point correctedWindowTopLeft = DisplayPlacementMath.PreserveAnchor(
            originalWindowTopLeft,
            new Point(resizedPanel.Right, resizedPanel.Top),
            desiredPanelTopRight);

        int offsetX = correctedWindowTopLeft.X - originalWindowTopLeft.X;
        int offsetY = correctedWindowTopLeft.Y - originalWindowTopLeft.Y;
        Point correctedPanelTopRight = new(
            resizedPanel.Right + offsetX,
            resizedPanel.Top + offsetY);

        Assert.InRange(fontSize, 32, 128);
        Assert.Equal(desiredPanelTopRight, correctedPanelTopRight);
    }

    [Fact]
    public void RepeatedFontSizeChangesDoNotAccumulateControlPanelDrift()
    {
        Point desiredPanelTopRight = new(1800, 620);
        Point windowTopLeft = new(900, 250);
        Rectangle[] transientPanelBounds =
        [
            new(1300, 500, 420, 166),
            new(1410, 555, 420, 166),
            new(1250, 470, 420, 166),
            new(1380, 525, 420, 166),
        ];

        foreach (Rectangle panelBounds in transientPanelBounds)
        {
            Point corrected = DisplayPlacementMath.PreserveAnchor(
                windowTopLeft,
                new Point(panelBounds.Right, panelBounds.Top),
                desiredPanelTopRight);
            Point correction = new(corrected.X - windowTopLeft.X, corrected.Y - windowTopLeft.Y);
            Point correctedAnchor = new(
                panelBounds.Right + correction.X,
                panelBounds.Top + correction.Y);

            Assert.Equal(desiredPanelTopRight, correctedAnchor);
        }
    }

    [Fact]
    public void HotspotToolWindowExplicitlyClearsClickThroughStyle()
    {
        const long wsExTransparent = 0x00000020L;
        const long wsExToolWindow = 0x00000080L;
        const long wsExNoActivate = 0x08000000L;

        long styles = WindowStyleService.CalculateToolWindowExtendedStyle(wsExTransparent, noActivate: true);

        Assert.Equal(0, styles & wsExTransparent);
        Assert.NotEqual(0, styles & wsExToolWindow);
        Assert.NotEqual(0, styles & wsExNoActivate);
    }

    [Fact]
    public void VisualClockClickThroughStyleCanBeEnabledAndRemoved()
    {
        const long wsExTransparent = 0x00000020L;
        const long wsExNoActivate = 0x08000000L;

        long locked = WindowStyleService.CalculateClickThroughExtendedStyle(0, enabled: true);
        long unlocked = WindowStyleService.CalculateClickThroughExtendedStyle(locked, enabled: false);

        Assert.NotEqual(0, locked & wsExTransparent);
        Assert.NotEqual(0, locked & wsExNoActivate);
        Assert.Equal(0, unlocked & wsExTransparent);
        Assert.Equal(0, unlocked & wsExNoActivate);
    }
}
