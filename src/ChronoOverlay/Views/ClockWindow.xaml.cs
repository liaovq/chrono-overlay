using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ChronoOverlay.Services;
using ChronoOverlay.ViewModels;

namespace ChronoOverlay.Views;

public partial class ClockWindow : Window
{
    private readonly ClockViewModel _viewModel;
    private readonly SettingsService _settingsService;
    private readonly DisplayPlacementService _placementService;
    private readonly LockedHotspotWindow _hotspotWindow;
    private System.Windows.Point _mouseDownPosition;
    private bool _dragCandidate;
    private bool _allowClose;

    public ClockWindow(
        ClockViewModel viewModel,
        SettingsService settingsService,
        DisplayPlacementService placementService)
    {
        _viewModel = viewModel;
        _settingsService = settingsService;
        _placementService = placementService;
        _hotspotWindow = new LockedHotspotWindow();

        InitializeComponent();
        DataContext = viewModel;
        SourceInitialized += OnSourceInitialized;
        ContentRendered += OnContentRendered;
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
        Closing += OnClosing;
        _hotspotWindow.UnlockRequested += (_, _) => UnlockAndShowPanel();
        _viewModel.LockRequested += (_, _) => Lock();
    }

    public event EventHandler? ExitRequested;

    public void Lock()
    {
        if (_viewModel.IsLocked && _hotspotWindow.IsVisible)
        {
            return;
        }

        _viewModel.SetLocked(true);
        ControlPanel.Visibility = Visibility.Collapsed;
        UpdateLayout();
        WindowStyleService.SetClickThrough(this, true);
        ShowHotspot();
        WindowStyleService.EnsureTopmost(this);
    }

    public void UnlockAndShowPanel()
    {
        _hotspotWindow.Hide();
        WindowStyleService.SetClickThrough(this, false);
        _viewModel.SetLocked(false);
        ControlPanel.Visibility = Visibility.Visible;
        UpdateLayout();
        Show();
        Activate();
        WindowStyleService.EnsureTopmost(this);
    }

    public void RequestExit()
    {
        _placementService.Capture(this, _viewModel.Settings);
        _settingsService.SaveImmediately(_viewModel.Settings);
        _allowClose = true;
        _hotspotWindow.Close();
        Close();
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SavePlacement()
    {
        _placementService.Capture(this, _viewModel.Settings);
        _settingsService.SaveImmediately(_viewModel.Settings);
    }

    public void EnsureVisibleAndTopmost()
    {
        _placementService.EnsureVisible(this);
        WindowStyleService.EnsureTopmost(this);
        if (_viewModel.IsLocked)
        {
            SyncHotspot();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs eventArgs) =>
        WindowStyleService.ConfigureToolWindow(this);

    private void OnContentRendered(object? sender, EventArgs eventArgs)
    {
        _placementService.Restore(this, _viewModel.Settings);
        if (_viewModel.IsLocked)
        {
            ControlPanel.Visibility = Visibility.Collapsed;
            UpdateLayout();
            WindowStyleService.SetClickThrough(this, true);
            ShowHotspot();
        }
    }

    private void ShowHotspot()
    {
        if (!_hotspotWindow.IsVisible)
        {
            _hotspotWindow.Show();
        }

        Dispatcher.BeginInvoke(SyncHotspot);
    }

    private void SyncHotspot()
    {
        if (!_viewModel.IsLocked || !IsLoaded || !TimeText.IsVisible)
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

    private void OnLocationChanged(object? sender, EventArgs eventArgs)
    {
        if (_viewModel.IsLocked)
        {
            SyncHotspot();
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs eventArgs)
    {
        if (_viewModel.IsLocked)
        {
            Dispatcher.BeginInvoke(SyncHotspot);
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
            _placementService.EnsureVisible(this);
            SavePlacement();
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
}
