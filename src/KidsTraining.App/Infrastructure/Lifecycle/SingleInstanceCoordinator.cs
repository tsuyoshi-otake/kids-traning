namespace KidsTraining.App.Infrastructure.Lifecycle;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private const string InstanceMutexName = @"Local\KidsTraining.App.Instance";
    private const string TrainingRequestEventName = @"Local\KidsTraining.App.TrainingRequest";

    private readonly Mutex instanceMutex;
    private readonly EventWaitHandle trainingRequestEvent;
    private readonly EventWaitHandle stopEvent = new(false, EventResetMode.ManualReset);
    private readonly object listenerGate = new();
    private Thread? listenerThread;
    private int disposed;

    private SingleInstanceCoordinator(
        Mutex instanceMutex,
        EventWaitHandle trainingRequestEvent,
        bool isPrimary)
    {
        this.instanceMutex = instanceMutex;
        this.trainingRequestEvent = trainingRequestEvent;
        IsPrimary = isPrimary;
    }

    public bool IsPrimary { get; }

    public static SingleInstanceCoordinator Acquire()
    {
        Mutex? instanceMutex = null;
        EventWaitHandle? trainingRequestEvent = null;
        var ownsMutex = false;

        try
        {
            instanceMutex = new Mutex(
                initiallyOwned: true,
                InstanceMutexName,
                out ownsMutex);
            trainingRequestEvent = new EventWaitHandle(
                initialState: false,
                EventResetMode.AutoReset,
                TrainingRequestEventName);
            return new SingleInstanceCoordinator(instanceMutex, trainingRequestEvent, ownsMutex);
        }
        catch
        {
            trainingRequestEvent?.Dispose();
            if (ownsMutex)
            {
                instanceMutex?.ReleaseMutex();
            }

            instanceMutex?.Dispose();
            throw;
        }
    }

    public bool SignalTrainingRequest()
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            return false;
        }

        try
        {
            return trainingRequestEvent.Set();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    public void StartListening(Action onTrainingRequested)
    {
        ArgumentNullException.ThrowIfNull(onTrainingRequested);
        if (!IsPrimary)
        {
            throw new InvalidOperationException("Only the primary application instance can listen for requests.");
        }

        lock (listenerGate)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
            if (listenerThread is not null)
            {
                throw new InvalidOperationException("The single-instance request listener has already started.");
            }

            listenerThread = new Thread(() => Listen(onTrainingRequested))
            {
                IsBackground = true,
                Name = "KidsTraining.SingleInstanceListener"
            };
            listenerThread.Start();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        stopEvent.Set();
        Thread? thread;
        lock (listenerGate)
        {
            thread = listenerThread;
        }

        if (thread is not null &&
            thread != Thread.CurrentThread &&
            !thread.Join(TimeSpan.FromSeconds(2)))
        {
            UpdateLogger.Info("Single-instance request listener did not stop within two seconds.");
        }

        trainingRequestEvent.Dispose();
        stopEvent.Dispose();
        if (IsPrimary)
        {
            try
            {
                instanceMutex.ReleaseMutex();
            }
            catch (ApplicationException exception)
            {
                UpdateLogger.Error("The primary-instance mutex was not owned during shutdown", exception);
            }
        }

        instanceMutex.Dispose();
    }

    private void Listen(Action onTrainingRequested)
    {
        var waitHandles = new WaitHandle[] { trainingRequestEvent, stopEvent };
        while (Volatile.Read(ref disposed) == 0)
        {
            int signaled;
            try
            {
                signaled = WaitHandle.WaitAny(waitHandles);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (signaled == 1 || Volatile.Read(ref disposed) != 0)
            {
                return;
            }

            try
            {
                onTrainingRequested();
            }
            catch (Exception exception)
            {
                UpdateLogger.Error("Could not dispatch a training request from another instance", exception);
            }
        }
    }
}
