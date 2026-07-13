using System.Drawing;
using System.Windows;
using ChronoOverlay.ViewModels;
using ChronoOverlay.Views;
using Forms = System.Windows.Forms;

namespace ChronoOverlay.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _lockItem;
    private readonly Forms.ToolStripMenuItem _autoStartItem;
    private readonly ClockWindow _window;
    private readonly ClockViewModel _viewModel;
    private readonly AutoStartService _autoStartService;

    public TrayIconService(ClockWindow window, ClockViewModel viewModel, AutoStartService autoStartService)
    {
        _window = window;
        _viewModel = viewModel;
        _autoStartService = autoStartService;

        Forms.ContextMenuStrip menu = new();
        Forms.ToolStripMenuItem showItem = new("显示面板 / 解锁");
        showItem.Click += (_, _) => Dispatch(_window.UnlockAndShowPanel);

        _lockItem = new Forms.ToolStripMenuItem("锁定") { CheckOnClick = false };
        _lockItem.Click += (_, _) => Dispatch(_window.Lock);

        _autoStartItem = new Forms.ToolStripMenuItem("开机自启") { CheckOnClick = false };
        _autoStartItem.Click += (_, _) => Dispatch(() => _viewModel.AutoStartEnabled = !_viewModel.AutoStartEnabled);

        Forms.ToolStripMenuItem exitItem = new("退出程序");
        exitItem.Click += (_, _) => Dispatch(_window.RequestExit);

        menu.Items.AddRange([showItem, _lockItem, _autoStartItem, new Forms.ToolStripSeparator(), exitItem]);
        menu.Opening += (_, _) => SyncMenuState();

        System.Windows.Resources.StreamResourceInfo? iconResource = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/ChronoOverlay.ico"));
        Icon icon = iconResource is not null ? new Icon(iconResource.Stream) : SystemIcons.Application;
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = BuildTooltip(),
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => Dispatch(_window.UnlockAndShowPanel);
        _viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName is nameof(ClockViewModel.AutoStartEnabled) or nameof(ClockViewModel.IsLocked))
            {
                SyncMenuState();
            }
        };
        _viewModel.AutoStartChanged += OnAutoStartChanged;
    }

    private static string BuildTooltip()
    {
        string tooltip = $"ChronoOverlay v{VersionService.Current}";
        return tooltip.Length <= 63 ? tooltip : "ChronoOverlay";
    }

    private void OnAutoStartChanged(object? sender, bool enabled)
    {
        try
        {
            _autoStartService.SetEnabled(enabled);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or InvalidOperationException)
        {
            _viewModel.RefreshAutoStart(_autoStartService.IsEnabled());
            System.Windows.MessageBox.Show(
                $"无法更新开机自启设置。\n\n{exception.Message}",
                "ChronoOverlay",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SyncMenuState()
    {
        _lockItem.Checked = _viewModel.IsLocked;
        _autoStartItem.Checked = _viewModel.AutoStartEnabled;
    }

    private static void Dispatch(Action action) => System.Windows.Application.Current.Dispatcher.BeginInvoke(action);

    public void Dispose()
    {
        _viewModel.AutoStartChanged -= OnAutoStartChanged;
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
    }
}
