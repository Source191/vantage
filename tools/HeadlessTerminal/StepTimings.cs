namespace Source191.Vantage.HeadlessTerminal;

/// <summary>How long to let the program settle after a marker before sending keys, and how long it has to exit after the last step.</summary>
public sealed record StepTimings(TimeSpan Settle, TimeSpan Grace)
{
    public static readonly StepTimings Default = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(6));
}