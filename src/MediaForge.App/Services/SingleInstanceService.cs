using Microsoft.UI.Dispatching;

namespace MediaForge.App.Services;

/// <summary>Prevents concurrent portable instances and asks the existing instance to activate its window.</summary>
public sealed class SingleInstanceService : IDisposable
{
    private const string MutexName = "Local\\MediaForge.SingleInstance";
    private const string ActivationEventName = "Local\\MediaForge.SingleInstance.Activate";
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationEvent;
    private readonly CancellationTokenSource _stopping = new();
    private Task? _listener;

    private SingleInstanceService(Mutex mutex, EventWaitHandle activationEvent, bool ownsInstance)
    {
        _mutex = mutex;
        _activationEvent = activationEvent;
        OwnsInstance = ownsInstance;
    }

    public bool OwnsInstance { get; }

    public static SingleInstanceService Acquire()
    {
        var mutex = new Mutex(true, MutexName, out var ownsInstance);
        var activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        return new SingleInstanceService(mutex, activationEvent, ownsInstance);
    }

    public void SignalExistingInstance()
    {
        if (!OwnsInstance) _activationEvent.Set();
    }

    public void ListenForActivation(DispatcherQueue dispatcherQueue, Action activate)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        ArgumentNullException.ThrowIfNull(activate);
        if (!OwnsInstance || _listener is not null) return;

        _listener = Task.Run(() =>
        {
            var handles = new WaitHandle[] { _activationEvent, _stopping.Token.WaitHandle };
            while (WaitHandle.WaitAny(handles) == 0)
            {
                dispatcherQueue.TryEnqueue(() => activate());
            }
        });
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _activationEvent.Dispose();
        _mutex.Dispose();
        _stopping.Dispose();
    }
}
