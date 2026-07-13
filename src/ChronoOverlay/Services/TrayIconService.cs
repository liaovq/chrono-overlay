using System.Drawing;
using System.Windows;
using ChronoOverlay.ViewModels;
using ChronoOverlay.Views;
using Forms = System.Windows.Forms;

namespace ChronoOverlay.Services;

public sealed class TrayIconService : IDisposable
{
    public static class MenuText
    {
        public const string Show = "显示面板 / 解锁";
        public const string Lock = "锁定";
        public const string AutoStart = "开机自启";
        public const string ThirdPartyLicenses = "第三方许可";
        public const string Exit = "退出程序";
    }

    public static IReadOnlyList<string> MenuItemTexts { get; } =
    [
        MenuText.Show,
        MenuText.Lock,
        MenuText.AutoStart,
        MenuText.ThirdPartyLicenses,
        MenuText.Exit,
    ];

    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _lockItem;
    private readonly Forms.ToolStripMenuItem _autoStartItem;
    private readonly ClockWindow _window;
    private readonly ClockViewModel _viewModel;
    private readonly AutoStartService _autoStartService;
    private readonly ThirdPartyLicenseWindowService _licenseWindowService;

    public TrayIconService(
        ClockWindow window,
        ClockViewModel viewModel,
        AutoStartService autoStartService,
        ThirdPartyLicenseWindowService licenseWindowService)
    {
        _window = window;
        _viewModel = viewModel;
        _autoStartService = autoStartService;
        _licenseWindowService = licenseWindowService;

        Forms.ContextMenuStrip menu = new();
        Forms.ToolStripMenuItem showItem = new(MenuText.Show);
        showItem.Click += (_, _) => Dispatch(_window.UnlockAndShowPanel);

        _lockItem = new Forms.ToolStripMenuItem(MenuText.Lock) { CheckOnClick = false };
        _lockItem.Click += (_, _) => Dispatch(_window.Lock);

        _autoStartItem = new Forms.ToolStripMenuItem(MenuText.AutoStart) { CheckOnClick = false };
        _autoStartItem.Click += (_, _) => Dispatch(() => _viewModel.AutoStartEnabled = !_viewModel.AutoStartEnabled);

        Forms.ToolStripMenuItem licensesItem = new(MenuText.ThirdPartyLicenses);
        licensesItem.Click += (_, _) => Dispatch(_licenseWindowService.Show);

        Forms.ToolStripMenuItem exitItem = new(MenuText.Exit);
        exitItem.Click += (_, _) => Dispatch(_window.RequestExit);

        menu.Items.AddRange(
        [
            showItem,
            _lockItem,
            _autoStartItem,
            new Forms.ToolStripSeparator(),
            licensesItem,
            new Forms.ToolStripSeparator(),
            exitItem,
        ]);
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
