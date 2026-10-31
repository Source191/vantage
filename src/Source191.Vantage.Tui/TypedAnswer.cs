namespace Source191.Vantage.Tui;

/// <summary>What a dialog that asks for typed text answered: the text, or that it was cancelled.</summary>
internal abstract record TypedAnswer
{
    public sealed record Typed(string Text) : TypedAnswer;

    /// <summary>Esc or Cancel closed the dialog.</summary>
    public sealed record Cancelled : TypedAnswer;
}