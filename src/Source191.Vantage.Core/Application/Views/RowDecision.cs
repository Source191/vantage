namespace Source191.Vantage.Application.Views;

/// <summary>What a row key decides about the selected row: run an effect, or explain why nothing happens.</summary>
internal abstract record RowDecision
{
    /// <summary>Run the effect; the status message is cleared.</summary>
    public sealed record Act(ViewEffect Effect) : RowDecision;

    /// <summary>Run nothing and show the message; <see cref="StatusMessage.None"/> for a row whose answer is still loading.</summary>
    public sealed record Explain(StatusMessage Message) : RowDecision;
}