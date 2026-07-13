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
using DrawingRectangle = System.Drawing.Rectangle;

namespace ChronoOverlay.Views;

public partial class ClockWindow : Window
{
    private const double ControlPanelGapDip = 6;
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
    private bool _panelPlacementPending;
    private ControlPanelPlacement _controlPanelPlacement = ControlPanelPlacement.Below;

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
        bool panelPlacementChanged = !_layoutTransition && RepositionControlPanelPreservingClockAnchor();
        bool corrected = !_layoutTransition &&
            _placementService.EnsureVisible(this, ClockSurface, _viewModel.Settings);
        if (corrected && !_viewModel.IsLocked)
        {
            panelPlacementChanged |= RepositionControlPanelPreservingClockAnchor();
        }

        if (!_layoutTransition && (corrected || panelPlacementChanged || persistPlacement))
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
        if (!locked)
        {
            ApplyBestControlPanelPlacement(desiredAnchor);
            UpdateLayout();
        }

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
            bool panelPlacementChanged = RepositionControlPanelPreservingClockAnchor();
            if (requiresPersistence || panelPlacementChanged)
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
            else
            {
                RepositionControlPanelPreservingClockAnchor();
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

        DrawingRectangle bounds = DisplayPlacementService.GetElementPhysicalRect(TimeText);
        WindowStyleService.SetPhysicalBounds(
            _hotspotWindow,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height);
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
        else if (!_layoutTransition)
        {
            ScheduleControlPanelPlacement();
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
            RepositionControlPanelPreservingClockAnchor();
            _placementService.EnsureVisible(this, ClockSurface, _viewModel.Settings);
            RepositionControlPanelPreservingClockAnchor();
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
            if (current is System.Windows.Controls.Primitives.ButtonBase or
                System.Windows.Controls.Slider or
                System.Windows.Controls.CheckBox or
                System.Windows.Controls.ComboBox or
                System.Windows.Controls.ComboBoxItem)
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

    private void ScheduleControlPanelPlacement()
    {
        if (_panelPlacementPending || !IsLoaded || _viewModel.IsLocked)
        {
            return;
        }

        _panelPlacementPending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            _panelPlacementPending = false;
            if (RepositionControlPanelPreservingClockAnchor())
            {
                CaptureAndSavePlacement();
            }
        }));
    }

    private bool RepositionControlPanelPreservingClockAnchor()
    {
        if (_layoutTransition || _viewModel.IsLocked || !IsLoaded || !ControlPanel.IsVisible)
        {
            return false;
        }

        DrawingPoint desiredAnchor = DisplayPlacementService.GetClockAnchorPhysical(ClockSurface);
        _layoutTransition = true;
        bool changed;
        try
        {
            UpdateLayout();
            changed = ApplyBestControlPanelPlacement(desiredAnchor);
            if (changed)
            {
                UpdateLayout();
                _placementService.PreserveClockAnchor(this, ClockSurface, desiredAnchor);
                UpdateLayout();
            }
        }
        finally
        {
            _layoutTransition = false;
        }

        return changed;
    }

    private bool ApplyBestControlPanelPlacement(DrawingPoint desiredAnchor)
    {
        DrawingRectangle currentClockBounds = DisplayPlacementService.GetElementPhysicalRect(ClockSurface);
        DrawingRectangle desiredClockBounds = new(
            desiredAnchor.X - currentClockBounds.Width,
            desiredAnchor.Y,
            currentClockBounds.Width,
            currentClockBounds.Height);
        MonitorWorkArea monitor = _placementService.GetMonitorForRectangle(desiredClockBounds);
        DpiScale dpi = VisualTreeHelper.GetDpi(ControlPanel);
        int panelHeight = Math.Max(1, (int)Math.Ceiling(ControlPanel.ActualHeight * dpi.DpiScaleY));
        int gap = Math.Max(0, (int)Math.Ceiling(ControlPanelGapDip * dpi.DpiScaleY));
        ControlPanelPlacement placement = ControlPanelPlacementMath.Resolve(
            monitor.WorkArea,
            desiredClockBounds,
            panelHeight,
            gap);

        if (_controlPanelPlacement == placement)
        {
            return false;
        }

        _controlPanelPlacement = placement;
        bool below = placement == ControlPanelPlacement.Below;
        Grid.SetRow(ClockSurface, below ? 0 : 1);
        Grid.SetRow(ControlPanel, below ? 1 : 0);
        ControlPanel.Margin = below
            ? new Thickness(0, ControlPanelGapDip, 0, 0)
            : new Thickness(0, 0, 0, ControlPanelGapDip);
        return true;
    }

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
