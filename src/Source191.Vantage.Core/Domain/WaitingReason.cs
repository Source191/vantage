namespace Source191.Vantage.Domain;

/// <summary>What a session waits for: nothing, or a reason in words ("permission prompt", in the CLI's own words).</summary>
public abstract record WaitingReason
{
    public sealed record None : WaitingReason;

    public sealed record Given(string Text) : WaitingReason;
}