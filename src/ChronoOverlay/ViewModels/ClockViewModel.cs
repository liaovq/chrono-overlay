using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using ChronoOverlay.Models;
using ChronoOverlay.Services;
using ChronoOverlay.Utilities;

namespace ChronoOverlay.ViewModels;

public sealed class ClockViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly ClockService _clockService;
    private readonly SettingsService _settingsService;
    private string _timeText = "00:00:00";
    private string _dateText = "0000.00.00 星期一";
    private bool _suppressSave;

    public ClockViewModel(AppSettings settings, ClockService clockService, SettingsService settingsService)
    {
        Settings = settings;
        _clockService = clockService;
        _settingsService = settingsService;
        _clockService.Tick += OnTick;
        _clockService.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LockRequested;

    public event EventHandler<bool>? AutoStartChanged;

    public AppSettings Settings { get; }

    public string TimeText
    {
        get => _timeText;
        private set => SetField(ref _timeText, value);
    }

    public string DateText
    {
        get => _dateText;
        private set => SetField(ref _dateText, value);
    }

    public double TimeFontSize
    {
        get => Settings.TimeFontSize;
        set
        {
            double normalized = Math.Clamp(value, AppSettings.MinTimeFontSize, AppSettings.MaxTimeFontSize);
            if (Math.Abs(Settings.TimeFontSize - normalized) < 0.01)
            {
                return;
            }

            Settings.TimeFontSize = normalized;
            Changed();
        }
    }

    public double DateFontSize
    {
        get => Settings.DateFontSize;
        set
        {
            double normalized = Math.Clamp(value, AppSettings.MinDateFontSize, AppSettings.MaxDateFontSize);
            if (Math.Abs(Settings.DateFontSize - normalized) < 0.01)
            {
                return;
            }

            Settings.DateFontSize = normalized;
            Changed();
        }
    }

    public double BackgroundOpacity
    {
        get => Settings.BackgroundOpacity;
        set
        {
            double normalized = Math.Clamp(value, 0, 1);
            if (Math.Abs(Settings.BackgroundOpacity - normalized) < 0.001)
            {
                return;
            }

            Settings.BackgroundOpacity = normalized;
            Changed();
            OnPropertyChanged(nameof(BackgroundPercent));
        }
    }

    public int BackgroundPercent => (int)Math.Round(BackgroundOpacity * 100);

    public double Hue
    {
        get => Settings.Hue;
        set
        {
            Settings.Hue = ColorUtilities.NormalizeHue(value);
            Settings.ColorMode = ColorMode.Hue;
            Settings.TextColor = ColorUtilities.ToHex(ColorUtilities.FromHue(Settings.Hue));
            Changed();
            OnPropertyChanged(nameof(TextBrush));
        }
    }

    public System.Windows.Media.Brush TextBrush
    {
        get
        {
            System.Windows.Media.Color color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Settings.TextColor);
            return new SolidColorBrush(color);
        }
    }

    public bool AutoStartEnabled
    {
        get => Settings.AutoStartEnabled;
        set
        {
            if (Settings.AutoStartEnabled == value)
            {
                return;
            }

            Settings.AutoStartEnabled = value;
            OnPropertyChanged();
            AutoStartChanged?.Invoke(this, value);
            SaveImmediately();
        }
    }

    public bool IsLocked => Settings.IsLocked;

    public void SetBlack() => SetColor(ColorMode.Black, "#000000");

    public void SetWhite() => SetColor(ColorMode.White, "#FFFFFF");

    public void RequestLock() => LockRequested?.Invoke(this, EventArgs.Empty);

    public void SetLocked(bool locked, bool saveImmediately = true)
    {
        if (Settings.IsLocked == locked)
        {
            return;
        }

        Settings.IsLocked = locked;
        OnPropertyChanged(nameof(IsLocked));
        if (saveImmediately)
        {
            SaveImmediately();
        }
    }

    public void RefreshAutoStart(bool enabled)
    {
        _suppressSave = true;
        Settings.AutoStartEnabled = enabled;
        OnPropertyChanged(nameof(AutoStartEnabled));
        _suppressSave = false;
    }

    public void SaveImmediately() => _settingsService.SaveImmediately(Settings);

    private void SetColor(ColorMode mode, string color)
    {
        Settings.ColorMode = mode;
        Settings.TextColor = color;
        OnPropertyChanged(nameof(TextBrush));
        _settingsService.SaveImmediately(Settings);
    }

    private void Changed([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(propertyName);
        if (!_suppressSave)
        {
            _settingsService.SaveDebounced(Settings);
        }
    }

    private void OnTick(object? sender, ClockTickEventArgs eventArgs)
    {
        TimeText = eventArgs.TimeText;
        DateText = eventArgs.DateText;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public void Dispose()
    {
        _clockService.Tick -= OnTick;
        _clockService.Dispose();
    }
}
