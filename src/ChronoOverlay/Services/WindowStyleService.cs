using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ChronoOverlay.Services;

public static partial class WindowStyleService
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020L;
    private const long WsExToolWindow = 0x00000080L;
    private const long WsExNoActivate = 0x08000000L;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoActivate = 0x0010;
    private static readonly nint HwndTopmost = new(-1);

    public static void ConfigureToolWindow(Window window, bool noActivate = false)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        long styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64() | WsExToolWindow;
        if (noActivate)
        {
            styles |= WsExNoActivate;
        }

        SetWindowLongPtr(handle, GwlExStyle, new nint(styles));
        EnsureTopmost(window);
    }

    public static void SetClickThrough(Window window, bool enabled)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        long styles = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        styles = enabled
            ? styles | WsExTransparent | WsExNoActivate
            : styles & ~WsExTransparent & ~WsExNoActivate;
        styles |= WsExToolWindow;
        SetWindowLongPtr(handle, GwlExStyle, new nint(styles));
    }

    public static void EnsureTopmost(Window window)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    public static void SetPhysicalBounds(Window window, int x, int y, int width, int height)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        SetWindowPos(handle, HwndTopmost, x, y, width, height, SwpNoActivate);
    }

    public static void SetPhysicalPosition(Window window, int x, int y)
    {
        nint handle = new WindowInteropHelper(window).Handle;
        SetWindowPos(handle, HwndTopmost, x, y, 0, 0, SwpNoSize | SwpNoActivate);
    }

    private static nint GetWindowLongPtr(nint handle, int index) => IntPtr.Size == 8
        ? GetWindowLongPtr64(handle, index)
        : new nint(GetWindowLong32(handle, index));

    private static nint SetWindowLongPtr(nint handle, int index, nint value) => IntPtr.Size == 8
        ? SetWindowLongPtr64(handle, index, value)
        : new nint(SetWindowLong32(handle, index, value.ToInt32()));

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static partial int GetWindowLong32(nint handle, int index);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static partial nint GetWindowLongPtr64(nint handle, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static partial int SetWindowLong32(nint handle, int index, int value);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static partial nint SetWindowLongPtr64(nint handle, int index, nint value);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint handle, nint insertAfter, int x, int y, int width, int height, uint flags);
}
