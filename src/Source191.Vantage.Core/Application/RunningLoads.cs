namespace Source191.Vantage.Application;

/// <summary>
/// One running worktrees scan at a time. The console reserves a scan on the UI thread before it triggers one, at
/// start-up, on a show of the tab and on the timer, and triggers nothing when <see cref="TryReserve"/> answers false;
/// the scan function itself is wrapped in <see cref="Track"/>, so the reload after an action, which never reserves,
/// is still counted. The reservation covers the gap between the trigger and the load starting on a pool thread. The
/// two counts are read and changed together, so they sit behind one lock.
/// </summary>
public sealed class RunningLoads
{
    private readonly Lock gate = new();
    private int reserved;
    private int running;

    /// <summary>True while a load is reserved or running.</summary>
    public bool IsRunning
    {
        get
        {
            lock (gate)
            {
                return reserved + running > 0;
            }
        }
    }

    /// <summary>Reserves a load and answers true when none is reserved or running; otherwise answers false and reserves nothing.</summary>
    public bool TryReserve()
    {
        lock (gate)
        {
            if (reserved + running > 0)
            {
                return false;
            }

            reserved++;

            return true;
        }
    }

    /// <summary>The load, counted as running from the call until it completes or throws, taking a held reservation as it starts.</summary>
    public Func<CancellationToken, Task<T>> Track<T>(Func<CancellationToken, Task<T>> load)
    {
        ArgumentNullException.ThrowIfNull(load);

        return async cancellationToken =>
        {
            Start();
            try
            {
                return await load(cancellationToken);
            }
            finally
            {
                Finish();
            }
        };
    }

    private void Start()
    {
        lock (gate)
        {
            running++;
            reserved = Math.Max(reserved - 1, 0);
        }
    }

    private void Finish()
    {
        lock (gate)
        {
            running--;
        }
    }
}