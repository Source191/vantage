namespace Source191.Vantage.Application;

/// <summary>
/// Numbers the loads a tab starts, on the UI thread, and drops the result of any load older than the last one
/// applied, so rows arrive in the order their loads began whatever order they finished in.
/// </summary>
public sealed class LoadSequence
{
    private int lastBegun;
    private int lastApplied;

    /// <summary>Hands out the number of a load that is about to start.</summary>
    public int Begin() => ++lastBegun;

    /// <summary>Answers true and remembers the ticket when no later load has been applied; otherwise answers false.</summary>
    public bool Accept(int ticket)
    {
        if (ticket <= lastApplied)
        {
            return false;
        }

        lastApplied = ticket;

        return true;
    }
}