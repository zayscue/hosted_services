using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class TimerAsync : ITimer
{
    private readonly Func<CancellationToken, Task> _scheduledAction;
    private readonly TimeSpan _dueTime;
    private readonly TimeSpan _period;
    private readonly CancellationTokenSource _interalCancellationSource;
    private CancellationTokenSource _cancellationSource;
    private Task _scheduledTask;
    private readonly SemaphoreSlim _semaphore;
    private bool _disposed;
    private readonly bool _canStartNextActionBeforePreviousIsCompleted;

    public event EventHandler<Exception> OnError;

    public bool IsRunning { get; private set; }


    public TimerAsync(Func<CancellationToken, Task> scheduledAction, TimeSpan dueTime, TimeSpan period, bool canStartNextActionBeforePreviousIsCompleted = false)
    {
        _scheduledAction = scheduledAction ?? throw new ArgumentNullException(nameof(scheduledAction));

        if (dueTime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(dueTime), "due time must be equal or greater than zero");
        _dueTime = dueTime;

        if (period < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period), "period must be equal or greater than zero");
        _period = period;

        _interalCancellationSource = new CancellationTokenSource();

        _canStartNextActionBeforePreviousIsCompleted = canStartNextActionBeforePreviousIsCompleted;

        _semaphore = new SemaphoreSlim(1);
    }

    public async Task Start(CancellationToken externalCancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (IsRunning)
                return;

            _cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                _interalCancellationSource.Token,
                externalCancellationToken
            );
            _scheduledTask = RunScheduledAction();
            IsRunning = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (IsRunning)
                return;

            _cancellationSource = _interalCancellationSource;

            _scheduledTask = RunScheduledAction();
            IsRunning = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Stop()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        await _semaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!IsRunning)
                return;

            _cancellationSource.Cancel();
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsRunning = false;
            _semaphore.Release();
        }
    }

    private Task RunScheduledAction()
    {
        return Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_dueTime, _cancellationSource.Token).ConfigureAwait(false);
                while (true)
                {
                    if (_canStartNextActionBeforePreviousIsCompleted)
                    {
#pragma warning disable 4014
                        _scheduledAction(_cancellationSource.Token);
#pragma warning restore 4014
                    }
                    else
                    {
                        await _scheduledAction(_cancellationSource.Token).ConfigureAwait(false);
                    }
                    await Task.Delay(_period, _cancellationSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                try
                {
                    OnError?.Invoke(this, ex);
                }
                catch { }
            }
            finally
            {
                IsRunning = false;
            }
        }, _cancellationSource.Token);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _interalCancellationSource?.Dispose();
            _cancellationSource?.Dispose();
            _semaphore?.Dispose();
        }

        _disposed = true;
    }

    ~TimerAsync()
    {
        Dispose(false);
    }
}