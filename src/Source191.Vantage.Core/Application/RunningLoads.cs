namespace Source191.Vantage.Application;

public sealed class RunningLoads
{
    private readonly Lock gate = new();
    private int reserved;
    private int running;

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

    public Func<CancellationToken, Task<T>> Track<T>(Func<CancellationToken, Task<T>> load) => async cancellationToken =>
    {
        lock (gate)
        {
            running++;
            if (reserved > 0)
            {
                reserved--;
            }
        }

        try
        {
            return await load(cancellationToken);
        }
        finally
        {
            lock (gate)
            {
                running--;
            }
        }
    };
}