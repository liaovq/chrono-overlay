using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace ChronoOverlay.Services;

public sealed class DisplayPlacementService
{
    private const double MinimumVisibleDip = 32;

    public void Restore(Window window, Models.AppSettings settings)
    {
        if (settings.WindowX is null || settings.WindowY is null)
        {
            PlaceAtPrimaryTopRight(window);
            return;
        }

        window.Left = settings.WindowX.Value;
        window.Top = settings.WindowY.Value;
        window.Dispatcher.BeginInvoke(() => EnsureVisible(window));
    }

    public void PlaceAtPrimaryTopRight(Window window)
    {
        Forms.Screen primary = Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];
        double scale = GetScale(window);
        window.Left = (primary.WorkingArea.Right / scale) - window.ActualWidth - 24;
        window.Top = (primary.WorkingArea.Top / scale) + 24;
    }

    public void EnsureVisible(Window window)
    {
        double scale = GetScale(window);
        Rectangle windowRect = new(
            (int)Math.Round(window.Left * scale),
            (int)Math.Round(window.Top * scale),
            Math.Max(1, (int)Math.Round(window.ActualWidth * scale)),
            Math.Max(1, (int)Math.Round(window.ActualHeight * scale)));

        int minimum = (int)Math.Round(MinimumVisibleDip * scale);
        if (IsVisibleOnAnyScreen(windowRect, Forms.Screen.AllScreens.Select(screen => screen.WorkingArea), minimum))
        {
            return;
        }

        PlaceAtPrimaryTopRight(window);
    }

    public void Capture(Window window, Models.AppSettings settings)
    {
        settings.WindowX = window.Left;
        settings.WindowY = window.Top;
        settings.SavedDpi = VisualTreeHelper.GetDpi(window).PixelsPerInchX;
        nint handle = new WindowInteropHelper(window).Handle;
        settings.MonitorDeviceName = Forms.Screen.FromHandle(handle).DeviceName;
    }

    public static bool IsVisibleOnAnyScreen(Rectangle window, IEnumerable<Rectangle> workAreas, int minimumVisible)
    {
        foreach (Rectangle area in workAreas)
        {
            Rectangle intersection = Rectangle.Intersect(window, area);
            if (intersection.Width >= minimumVisible && intersection.Height >= minimumVisible)
            {
                return true;
            }
        }

        return false;
    }

    private static double GetScale(Visual visual) => VisualTreeHelper.GetDpi(visual).DpiScaleX;
}
