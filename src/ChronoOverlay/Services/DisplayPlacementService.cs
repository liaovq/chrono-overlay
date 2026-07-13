using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using ChronoOverlay.Models;
using DrawingPoint = System.Drawing.Point;
using DrawingSize = System.Drawing.Size;

namespace ChronoOverlay.Services;

public sealed record MonitorWorkArea(string DeviceName, Rectangle WorkArea, uint Dpi, bool IsPrimary);

public sealed record PlacementResolution(DrawingPoint WindowTopLeft, MonitorWorkArea Monitor, bool RequiresPersistence);

public static class DisplayPlacementMath
{
    public const int MinimumVisiblePixels = 32;
    private const int DefaultMarginPixels = 24;

    public static PlacementResolution ResolveWindowPosition(
        AppSettings settings,
        IReadOnlyList<MonitorWorkArea> monitors,
        DrawingSize windowSize,
        DrawingPoint clockAnchorOffset)
    {
        if (monitors.Count == 0)
        {
            throw new ArgumentException("At least one monitor is required.", nameof(monitors));
        }

        MonitorWorkArea fallback = monitors.FirstOrDefault(monitor => monitor.IsPrimary) ?? monitors[0];
        MonitorWorkArea? savedMonitor = monitors.FirstOrDefault(monitor =>
            string.Equals(monitor.DeviceName, settings.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
        MonitorWorkArea target = savedMonitor ?? fallback;
        bool requiresPersistence = savedMonitor is null;
        DrawingPoint candidate;

        if (savedMonitor is not null &&
            settings.ClockAnchorOffsetX is int anchorX &&
            settings.ClockAnchorOffsetY is int anchorY)
        {
            int scaledX = ScaleForDpi(anchorX, settings.SavedDpi, target.Dpi);
            int scaledY = ScaleForDpi(anchorY, settings.SavedDpi, target.Dpi);
            candidate = new DrawingPoint(
                target.WorkArea.Left + scaledX - clockAnchorOffset.X,
                target.WorkArea.Top + scaledY - clockAnchorOffset.Y);
            requiresPersistence = Math.Abs(settings.SavedDpi - target.Dpi) >= 0.5;
        }
        else if (savedMonitor is not null &&
                 settings.WindowPhysicalX is int physicalX &&
                 settings.WindowPhysicalY is int physicalY)
        {
            candidate = new DrawingPoint(physicalX, physicalY);
            requiresPersistence = true;
        }
        else if (savedMonitor is not null && settings.WindowX is double legacyX && settings.WindowY is double legacyY)
        {
            double legacyScale = NormalizeDpi(settings.SavedDpi) / 96d;
            candidate = new DrawingPoint(
                (int)Math.Round(legacyX * legacyScale),
                (int)Math.Round(legacyY * legacyScale));
            requiresPersistence = true;
        }
        else
        {
            candidate = GetDefaultTopRight(target, windowSize, clockAnchorOffset);
            requiresPersistence = true;
        }

        Rectangle candidateRect = new(candidate, windowSize);
        if (!DisplayPlacementService.IsVisibleOnAnyScreen(
                candidateRect,
                [target.WorkArea],
                MinimumVisiblePixels))
        {
            candidate = GetDefaultTopRight(target, windowSize, clockAnchorOffset);
            requiresPersistence = true;
        }

        return new PlacementResolution(candidate, target, requiresPersistence);
    }

    public static int ScaleForDpi(int value, double savedDpi, uint targetDpi) =>
        (int)Math.Round(value * targetDpi / NormalizeDpi(savedDpi));

    public static DrawingPoint PreserveAnchor(
        DrawingPoint windowTopLeft,
        DrawingPoint currentAnchor,
        DrawingPoint desiredAnchor) =>
        new(
            windowTopLeft.X + desiredAnchor.X - currentAnchor.X,
            windowTopLeft.Y + desiredAnchor.Y - currentAnchor.Y);

    private static double NormalizeDpi(double dpi) => dpi is >= 48 and <= 768 ? dpi : 96;

    private static DrawingPoint GetDefaultTopRight(
        MonitorWorkArea monitor,
        DrawingSize windowSize,
        DrawingPoint clockAnchorOffset)
    {
        int desiredAnchorX = monitor.WorkArea.Right - DefaultMarginPixels;
        int desiredAnchorY = monitor.WorkArea.Top + DefaultMarginPixels;
        DrawingPoint candidate = new(
            desiredAnchorX - clockAnchorOffset.X,
            desiredAnchorY - clockAnchorOffset.Y);

        int maximumX = Math.Max(monitor.WorkArea.Left, monitor.WorkArea.Right - windowSize.Width);
        int maximumY = Math.Max(monitor.WorkArea.Top, monitor.WorkArea.Bottom - windowSize.Height);
        return new DrawingPoint(
            Math.Clamp(candidate.X, monitor.WorkArea.Left, maximumX),
            Math.Clamp(candidate.Y, monitor.WorkArea.Top, maximumY));
    }
}

public sealed class DisplayPlacementService
{
    private const uint MonitorDefaultToNearest = 2;
    private const uint MonitorInfoPrimary = 1;
    private const int EffectiveDpi = 0;

    public bool Restore(Window window, FrameworkElement clockSurface, AppSettings settings)
    {
        Rectangle windowRect = GetWindowPhysicalRect(window);
        DrawingPoint anchorOffset = GetClockAnchorOffset(windowRect, clockSurface);
        IReadOnlyList<MonitorWorkArea> monitors = GetMonitorWorkAreas();
        PlacementResolution resolution = DisplayPlacementMath.ResolveWindowPosition(
            settings,
            monitors,
            windowRect.Size,
            anchorOffset);

        bool moved = windowRect.Location != resolution.WindowTopLeft;
        if (moved)
        {
            WindowStyleService.SetPhysicalPosition(window, resolution.WindowTopLeft.X, resolution.WindowTopLeft.Y);
        }

        return moved || resolution.RequiresPersistence || settings.SchemaVersion < AppSettings.CurrentSchemaVersion;
    }

    public bool EnsureVisible(Window window, FrameworkElement clockSurface, AppSettings settings)
    {
        Rectangle windowRect = GetWindowPhysicalRect(window);
        IReadOnlyList<MonitorWorkArea> monitors = GetMonitorWorkAreas();
        if (IsVisibleOnAnyScreen(
                windowRect,
                monitors.Select(monitor => monitor.WorkArea),
                DisplayPlacementMath.MinimumVisiblePixels))
        {
            return false;
        }

        DrawingPoint anchorOffset = GetClockAnchorOffset(windowRect, clockSurface);
        PlacementResolution resolution = DisplayPlacementMath.ResolveWindowPosition(
            settings,
            monitors,
            windowRect.Size,
            anchorOffset);
        WindowStyleService.SetPhysicalPosition(window, resolution.WindowTopLeft.X, resolution.WindowTopLeft.Y);
        return true;
    }

    public void PreserveClockAnchor(Window window, FrameworkElement clockSurface, DrawingPoint desiredAnchor)
    {
        DrawingPoint currentAnchor = GetClockAnchorPhysical(clockSurface);
        Rectangle windowRect = GetWindowPhysicalRect(window);
        DrawingPoint target = DisplayPlacementMath.PreserveAnchor(windowRect.Location, currentAnchor, desiredAnchor);
        WindowStyleService.SetPhysicalPosition(window, target.X, target.Y);
    }

    public void Capture(Window window, FrameworkElement clockSurface, AppSettings settings)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        Rectangle windowRect = GetWindowPhysicalRect(window);
        MonitorWorkArea monitor = GetMonitorForWindow(handle);
        DrawingPoint anchor = GetClockAnchorPhysical(clockSurface);

        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        settings.WindowPhysicalX = windowRect.Left;
        settings.WindowPhysicalY = windowRect.Top;
        settings.WindowPhysicalWidth = windowRect.Width;
        settings.WindowPhysicalHeight = windowRect.Height;
        settings.ClockAnchorOffsetX = anchor.X - monitor.WorkArea.Left;
        settings.ClockAnchorOffsetY = anchor.Y - monitor.WorkArea.Top;
        settings.MonitorDeviceName = monitor.DeviceName;
        settings.SavedDpi = GetDpiForWindow(handle);

        // Retain the schema v1 fields for downgrade compatibility; schema v2 restoration never uses
        // them while physical placement data is available.
        settings.WindowX = window.Left;
        settings.WindowY = window.Top;
    }

    public static DrawingPoint GetClockAnchorPhysical(FrameworkElement clockSurface)
    {
        System.Windows.Point screenPoint = clockSurface.PointToScreen(
            new System.Windows.Point(clockSurface.ActualWidth, 0));
        return new DrawingPoint((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y));
    }

    public static Rectangle GetWindowPhysicalRect(Window window)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        if (!GetWindowRect(handle, out NativeRect rect))
        {
            throw new InvalidOperationException("Unable to read the ChronoOverlay window rectangle.");
        }

        return rect.ToRectangle();
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

    private static DrawingPoint GetClockAnchorOffset(Rectangle windowRect, FrameworkElement clockSurface)
    {
        DrawingPoint anchor = GetClockAnchorPhysical(clockSurface);
        return new DrawingPoint(anchor.X - windowRect.Left, anchor.Y - windowRect.Top);
    }

    private static IReadOnlyList<MonitorWorkArea> GetMonitorWorkAreas()
    {
        List<MonitorWorkArea> monitors = [];
        MonitorEnumProc callback = (monitorHandle, _, _, _) =>
        {
            monitors.Add(GetMonitor(monitorHandle));
            return true;
        };

        if (!EnumDisplayMonitors(0, 0, callback, 0) || monitors.Count == 0)
        {
            throw new InvalidOperationException("Unable to enumerate Windows display work areas.");
        }

        GC.KeepAlive(callback);
        return monitors;
    }

    private static MonitorWorkArea GetMonitorForWindow(nint windowHandle) =>
        GetMonitor(MonitorFromWindow(windowHandle, MonitorDefaultToNearest));

    private static MonitorWorkArea GetMonitor(nint monitorHandle)
    {
        MonitorInfo monitorInfo = new() { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
        {
            throw new InvalidOperationException("Unable to read Windows monitor information.");
        }

        uint dpi = 96;
        if (GetDpiForMonitor(monitorHandle, EffectiveDpi, out uint dpiX, out _) == 0)
        {
            dpi = dpiX;
        }

        return new MonitorWorkArea(
            monitorInfo.DeviceName,
            monitorInfo.WorkArea.ToRectangle(),
            dpi,
            (monitorInfo.Flags & MonitorInfoPrimary) != 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }

    private delegate bool MonitorEnumProc(nint monitor, nint deviceContext, nint monitorRect, nint data);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRect,
        MonitorEnumProc callback,
        nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window, uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(nint window);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);
}
