using System.Windows;
using ChronoOverlay.Services;
using ChronoOverlay.ViewModels;
using ChronoOverlay.Views;
using Microsoft.Win32;

namespace ChronoOverlay;

public partial class App : System.Windows.Application
{
    private SingleInstanceService? _singleInstanceService;
    private SettingsService? _settingsService;
    private ClockViewModel? _viewModel;
    private TrayIconService? _trayIconService;
    private ClockWindow? _clockWindow;

    protected override void OnStartup(StartupEventArgs eventArgs)
    {
        base.OnStartup(eventArgs);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _singleInstanceService = new SingleInstanceService();
        if (!_singleInstanceService.IsPrimary)
        {
            SingleInstanceService.SignalPrimary();
            Shutdown();
            return;
        }

        _settingsService = new SettingsService();
        Models.AppSettings settings = _settingsService.Load();
        AutoStartService autoStartService = new();
        ReconcileAutoStart(autoStartService, settings);

        _viewModel = new ClockViewModel(settings, new ClockService(), _settingsService);
        _clockWindow = new ClockWindow(_viewModel, _settingsService, new DisplayPlacementService());
        _trayIconService = new TrayIconService(_clockWindow, _viewModel, autoStartService);
        _clockWindow.ExitRequested += (_, _) => Shutdown();
        _settingsService.SaveFailed += OnSaveFailed;

        _singleInstanceService.StartListening(() => Dispatcher.BeginInvoke(_clockWindow.UnlockAndShowPanel));
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SessionEnding += OnSessionEnding;

        MainWindow = _clockWindow;
        _clockWindow.Show();
    }

    protected override void OnExit(ExitEventArgs eventArgs)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _trayIconService?.Dispose();
        _viewModel?.Dispose();
        _settingsService?.Dispose();
        _singleInstanceService?.Dispose();
        base.OnExit(eventArgs);
    }

    private static void ReconcileAutoStart(AutoStartService autoStartService, Models.AppSettings settings)
    {
        try
        {
            if (settings.AutoStartEnabled)
            {
                autoStartService.SetEnabled(true);
            }
            else if (autoStartService.IsEnabled())
            {
                settings.AutoStartEnabled = true;
                autoStartService.RepairIfEnabled();
            }
        }
        catch (Exception) when (settings.AutoStartEnabled)
        {
            settings.AutoStartEnabled = false;
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs eventArgs) =>
        Dispatcher.BeginInvoke(() => _clockWindow?.EnsureVisibleAndTopmost());

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs eventArgs)
    {
        if (eventArgs.Category is UserPreferenceCategory.Desktop or UserPreferenceCategory.Window)
        {
            Dispatcher.BeginInvoke(() => _clockWindow?.EnsureVisibleAndTopmost());
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs eventArgs)
    {
        if (eventArgs.Mode == PowerModes.Resume)
        {
            Dispatcher.BeginInvoke(() => _clockWindow?.EnsureVisibleAndTopmost());
        }
    }

    private void OnSessionEnding(object sender, SessionEndingCancelEventArgs eventArgs) =>
        _clockWindow?.SavePlacement();

    private void OnSaveFailed(object? sender, Exception exception) => Dispatcher.BeginInvoke(() =>
        System.Windows.MessageBox.Show(
            $"配置暂时无法保存，程序会继续运行。\n\n{exception.Message}",
            "ChronoOverlay",
            MessageBoxButton.OK,
            MessageBoxImage.Warning));
}
