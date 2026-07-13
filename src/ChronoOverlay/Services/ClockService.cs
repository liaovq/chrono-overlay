using System.Windows.Threading;
using ChronoOverlay.Utilities;

namespace ChronoOverlay.Services;

public sealed class ClockService : IDisposable
{
    private readonly DispatcherTimer _timer;

    public ClockService()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            IsEnabled = false,
        };
        _timer.Tick += OnTick;
    }

    public event EventHandler<ClockTickEventArgs>? Tick;

    public void Start()
    {
        PublishAndSchedule();
    }

    private void OnTick(object? sender, EventArgs eventArgs)
    {
        _timer.Stop();
        PublishAndSchedule();
    }

    private void PublishAndSchedule()
    {
        DateTime now = DateTime.Now;
        Tick?.Invoke(this, new ClockTickEventArgs(DateTimeFormatter.FormatTime(now), DateTimeFormatter.FormatDate(now)));

        DateTime nextSecond = now.AddSeconds(1);
        nextSecond = new DateTime(nextSecond.Year, nextSecond.Month, nextSecond.Day, nextSecond.Hour, nextSecond.Minute, nextSecond.Second, now.Kind);
        _timer.Interval = nextSecond - DateTime.Now;
        if (_timer.Interval < TimeSpan.FromMilliseconds(10))
        {
            _timer.Interval = TimeSpan.FromMilliseconds(10);
        }

        _timer.Start();
    }

    public void Dispose() => _timer.Stop();
}

public sealed record ClockTickEventArgs(string TimeText, string DateText);
