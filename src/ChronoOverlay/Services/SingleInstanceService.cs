using System.Threading;

namespace ChronoOverlay.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\ChronoOverlay.SingleInstance";
    private const string WakeEventName = @"Local\ChronoOverlay.Wake";
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _wakeEvent;
    private RegisteredWaitHandle? _registration;
    private bool _ownsMutex;

    public SingleInstanceService()
        : this(MutexName, WakeEventName)
    {
    }

    internal SingleInstanceService(string mutexName, string wakeEventName)
    {
        _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, wakeEventName);
        _mutex = new Mutex(true, mutexName, out _ownsMutex);
    }

    public bool IsPrimary => _ownsMutex;

    public void StartListening(Action onWake)
    {
        if (!IsPrimary)
        {
            throw new InvalidOperationException("Only the primary instance can listen.");
        }

        _registration = ThreadPool.RegisterWaitForSingleObject(
            _wakeEvent,
            (_, _) => onWake(),
            null,
            Timeout.Infinite,
            false);
    }

    public void SignalPrimary() => _wakeEvent.Set();

    public void Dispose()
    {
        _registration?.Unregister(null);
        _wakeEvent.Dispose();
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex.Dispose();
    }
}
