using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>Whether claude stopped the session.</summary>
public abstract record StopSessionOutcome : Outcome
{
    /// <summary>The session was stopped; it stays listed and can be resumed. TabClosed when a Vantage tab showing it was closed too.</summary>
    public sealed record Stopped(string Name, bool TabClosed) : StopSessionOutcome;

    /// <summary>Claude refused with this sentence; the session still runs and its tab stays open.</summary>
    public sealed record NotStopped(string Name, string Failure) : StopSessionOutcome;
}