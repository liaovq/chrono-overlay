using System.Threading;

namespace ChronoOverlay.Services;

public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = @"Local\ChronoOverlay.SingleInstance";
    private const string WakeEventName = @"Local\ChronoOverlay.Wake";
    private readonly Mutex _mutex;
    private EventWaitHandle? _wakeEvent;
    private RegisteredWaitHandle? _registration;
    private bool _ownsMutex;

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, MutexName, out _ownsMutex);
    }

    public bool IsPrimary => _ownsMutex;

    public void StartListening(Action onWake)
    {
        if (!IsPrimary)
        {
            throw new InvalidOperationException("Only the primary instance can listen.");
        }

        _wakeEvent = new EventWaitHandle(false, EventResetMode.AutoReset, WakeEventName);
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _wakeEvent,
            (_, _) => onWake(),
            null,
            Timeout.Infinite,
            false);
    }

    public static void SignalPrimary()
    {
        try
        {
            using EventWaitHandle wakeEvent = EventWaitHandle.OpenExisting(WakeEventName);
            wakeEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // The primary process may still be starting. A second launch should still exit.
        }
    }

    public void Dispose()
    {
        _registration?.Unregister(null);
        _wakeEvent?.Dispose();
        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
            _ownsMutex = false;
        }

        _mutex.Dispose();
    }
}
