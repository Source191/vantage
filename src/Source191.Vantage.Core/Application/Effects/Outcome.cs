namespace Source191.Vantage.Application.Effects;

/// <summary>
/// What a handler did, as facts rather than a sentence; <see cref="Wording"/> gives the words. Each handler's outcome
/// derives from this and is declared in its own file beside its handler.
/// </summary>
public abstract record Outcome;