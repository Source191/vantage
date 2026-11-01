namespace Source191.Vantage.Domain;

/// <summary>What a new session is first asked: nothing, so it waits at its own prompt, or the text typed (trimmed).</summary>
/// <remarks>The text is <c>Words</c>, not <c>Value</c>: a Domain record with a string <c>Value</c> is a text value to <c>TextValuesShould</c>.</remarks>
public abstract record OpeningPrompt
{
    public sealed record None : OpeningPrompt;

    public sealed record Text(string Words) : OpeningPrompt;
}