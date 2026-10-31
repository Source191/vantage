namespace Source191.Vantage.Application;

public sealed class RunningLoads
{
    private readonly Lock gate = new();
    private int reserved;

    public bool IsRunning
    {
        get
        {
            lock (gate)
            {
                return reserved > 0;
            }
        }
    }

    public bool TryReserve()
    {
        lock (gate)
        {
            if (reserved > 0)
            {
                return false;
            }

            reserved++;

            return true;
        }
    }
}