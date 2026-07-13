using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ChronoOverlay.Services;
using ChronoOverlay.ViewModels;
using DrawingPoint = System.Drawing.Point;

namespace ChronoOverlay.Views;

public partial class ClockWindow : Window
{
    private const int WmDpiChanged = 0x02E0;
    private readonly ClockViewModel _viewModel;
    private readonly SettingsService _settingsService;
    private readonly DisplayPlacementService _placementService;
    private readonly LockedHotspotWindow _hotspotWindow;
    private readonly StartupMode _startupMode;
    private System.Windows.Point _mouseDownPosition;
    private HwndSource? _hwndSource;
    private bool _dragCandidate;
    private bool _allowClose;
    private bool _layoutTransition;

    public ClockWindow(
        ClockViewModel viewModel,
        SettingsService settingsService,
        DisplayPlacementService placementService,
        StartupMode startupMode)
    {
        _viewModel = viewModel;
        _settingsService = settingsService;
        _placementService = placementService;
        _startupMode = startupMode;
        _hotspotWindow = new LockedHotspotWindow();

        InitializeComponent();
        DataContext = viewModel;
        ShowActivated = startupMode == StartupMode.Interactive && !viewModel.IsLocked;
        if (viewModel.IsLocked)
        {
            ControlPanel.Visibility = Visibility.Collapsed;
        }

        SourceInitialized += OnSourceInitialized;
        ContentRendered += OnContentRendered;
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
        Closing += OnClosing;
        Closed += OnClosed;
        _hotspotWindow.UnlockRequested += (_, _) => UnlockAndShowPanel();
        _viewModel.LockRequested += (_, _) => Lock();
    }

    public event EventHandler? ExitRequested;

    public void ShowForStartup()
    {
        Show();
        if (_startupMode == StartupMode.Interactive && !_viewModel.IsLocked)
        {
            Activate();
        }
    }

    public void Lock() => TransitionLockState(true, activateWhenUnlocked: false);

    public void UnlockAndShowPanel() => TransitionLockState(false, activateWhenUnlocked: true);

    public void RequestExit()
    {
        SavePlacement();
        _allowClose = true;
        _hotspotWindow.Close();
        Close();
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SavePlacement()
    {
        if (IsLoaded && !_layoutTransition)
        {
            _placementService.Capture(this, ClockSurface, _viewModel.Settings);
        }

        _settingsService.SaveImmediately(_viewModel.Settings);
    }

    public void EnsureVisibleAndTopmost(bool persistPlacement = false)
    {
        bool corrected = !_layoutTransition &&
            _placementService.EnsureVisible(this, ClockSurface, _viewModel.Settings);
        if (!_layoutTransition && (corrected || persistPlacement))
        {
            CaptureAndSavePlacement();
        }

        WindowStyleService.EnsureTopmost(this);
        if (_viewModel.IsLocked && !_layoutTransition)
        {
            SyncHotspot();
        }
    }

    private void TransitionLockState(bool locked, bool activateWhenUnlocked)
    {
        if (_layoutTransition || _viewModel.IsLocked == locked)
        {
            if (!locked && activateWhenUnlocked)
            {
                Show();
                Activate();
            }

            return;
        }

        DrawingPoint desiredAnchor = DisplayPlacementService.GetClockAnchorPhysical(ClockSurface);
        _layoutTransition = true;

        if (!locked)
        {
            _hotspotWindow.Hide();
            WindowStyleService.SetClickThrough(this, false);
        }

        _viewModel.SetLocked(locked, saveImmediately: false);
        ControlPanel.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        UpdateLayout();

        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            UpdateLayout();
            _placementService.PreserveClockAnchor(this, ClockSurface, desiredAnchor);
            UpdateLayout();

            if (locked)
            {
                WindowStyleService.SetClickThrough(this, true);
                ShowHotspot();
            }
            else
            {
                Show();
                if (activateWhenUnlocked)
                {
                    Activate();
                }
            }

            WindowStyleService.EnsureTopmost(this);
            _layoutTransition = false;
            CaptureAndSavePlacement();
        }));
    }

    private void OnSourceInitialized(object? sender, EventArgs eventArgs)
    {
        WindowStyleService.ConfigureToolWindow(this);
        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _hwndSource?.AddHook(WindowMessageHook);

        if (_viewModel.IsLocked)
        {
            WindowStyleService.SetClickThrough(this, true);
        }
    }

    private void OnContentRendered(object? sender, EventArgs eventArgs)
    {
        bool requiresPersistence = _placementService.Restore(this, ClockSurface, _viewModel.Settings);
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            UpdateLayout();
            if (requiresPersistence)
            {
                CaptureAndSavePlacement();
            }

            if (_viewModel.IsLocked)
            {
                ShowHotspot();
            }

            WindowStyleService.EnsureTopmost(this);
        }));
    }

    private nint WindowMessageHook(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmDpiChanged || lParam == 0)
        {
            return 0;
        }

        NativeRect suggested = Marshal.PtrToStructure<NativeRect>(lParam);
        _layoutTransition = true;
        WindowStyleService.SetPhysicalBounds(
            this,
            suggested.Left,
            suggested.Top,
            suggested.Right - suggested.Left,
            suggested.Bottom - suggested.Top);

        // Leave the message unhandled so WPF updates its PerMonitorV2 layout metrics. The Win32
        // suggested rectangle has already been applied before the next render pass.
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            UpdateLayout();
            _layoutTransition = false;
            if (_viewModel.IsLocked)
            {
                SyncHotspot();
            }

            CaptureAndSavePlacement();
        }));
        return 0;
    }

    private void ShowHotspot()
    {
        if (!_hotspotWindow.IsVisible)
        {
            _hotspotWindow.Show();
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(SyncHotspot));
    }

    private void SyncHotspot()
    {
        if (!_viewModel.IsLocked || _layoutTransition || !IsLoaded || !TimeText.IsVisible)
        {
            return;
        }

        System.Windows.Point topLeft = TimeText.PointToScreen(new System.Windows.Point(0, 0));
        DpiScale dpi = VisualTreeHelper.GetDpi(TimeText);
        int width = Math.Max(1, (int)Math.Ceiling(TimeText.ActualWidth * dpi.DpiScaleX));
        int height = Math.Max(1, (int)Math.Ceiling(TimeText.ActualHeight * dpi.DpiScaleY));
        WindowStyleService.SetPhysicalBounds(
            _hotspotWindow,
            (int)Math.Round(topLeft.X),
            (int)Math.Round(topLeft.Y),
            width,
            height);
    }

    private void CaptureAndSavePlacement()
    {
        if (!IsLoaded || _layoutTransition)
        {
            return;
        }

        SavePlacement();
    }

    private void OnLocationChanged(object? sender, EventArgs eventArgs)
    {
        if (_viewModel.IsLocked && !_layoutTransition)
        {
            SyncHotspot();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs eventArgs)
    {
        if (_viewModel.IsLocked && !_layoutTransition)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(SyncHotspot));
        }
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (_viewModel.IsLocked || IsInteractiveElement(eventArgs.OriginalSource as DependencyObject))
        {
            return;
        }

        _mouseDownPosition = eventArgs.GetPosition(this);
        _dragCandidate = true;
        CaptureMouse();
    }

    private void OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs eventArgs)
    {
        if (!_dragCandidate || eventArgs.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        System.Windows.Point current = eventArgs.GetPosition(this);
        if (Math.Abs(current.X - _mouseDownPosition.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - _mouseDownPosition.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _dragCandidate = false;
        ReleaseMouseCapture();
        try
        {
            DragMove();
            _placementService.EnsureVisible(this, ClockSurface, _viewModel.Settings);
            CaptureAndSavePlacement();
        }
        catch (InvalidOperationException)
        {
            // The mouse button was released between threshold detection and DragMove.
        }
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs eventArgs)
    {
        _dragCandidate = false;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        for (DependencyObject? current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is System.Windows.Controls.Primitives.ButtonBase or Slider or System.Windows.Controls.CheckBox)
            {
                return true;
            }
        }

        return false;
    }

    private void OnTimeMouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (!_viewModel.IsLocked && eventArgs.ClickCount == 2 && eventArgs.ChangedButton == MouseButton.Left)
        {
            Lock();
            eventArgs.Handled = true;
        }
    }

    private void OnBlackClick(object sender, RoutedEventArgs eventArgs) => _viewModel.SetBlack();

    private void OnWhiteClick(object sender, RoutedEventArgs eventArgs) => _viewModel.SetWhite();

    private void OnLockClick(object sender, RoutedEventArgs eventArgs) => Lock();

    private void OnClosing(object? sender, CancelEventArgs eventArgs)
    {
        if (!_allowClose)
        {
            eventArgs.Cancel = true;
        }
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        _hwndSource?.RemoveHook(WindowMessageHook);
        _hwndSource = null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
