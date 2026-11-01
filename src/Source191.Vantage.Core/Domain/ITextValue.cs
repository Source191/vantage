using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>
/// A value that wraps text, with three doors: <see cref="From"/> builds it or throws, for text Vantage already trusts;
/// <see cref="TryParse"/> answers false for text from outside Vantage (git's output, the CLI's listing, a state file);
/// and a type that takes typed text adds a <c>Validate</c> that returns the reason it refuses.
/// </summary>
public interface ITextValue<TSelf>
    where TSelf : class, ITextValue<TSelf>
{
    string Value { get; }

    static abstract TSelf From(string text);

    /// <param name="text">The text from outside Vantage, which may be null.</param>
    /// <param name="value">The value when the method returns true; null exactly when it returns false.</param>
    static abstract bool TryParse(string? text, [MaybeNullWhen(false)] out TSelf value);
}