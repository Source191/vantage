using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>
/// A value that wraps text, with three doors: the constructor builds it or throws, for text Vantage already trusts;
/// <see cref="TryParse"/> answers false for text from outside Vantage (git's output, the CLI's listing, a state file);
/// and a type that takes typed text adds a <c>Validate</c> that returns the reason it refuses.
/// </summary>
public interface ITextValue<TSelf>
    where TSelf : class, ITextValue<TSelf>
{
    string Value { get; }

    static abstract bool TryParse(string? text, [MaybeNullWhen(false)] out TSelf value);
}

public static class TextValues
{
    /// <summary>The texts that parse, in order; the rest are skipped, as a listing skips a row it cannot read.</summary>
    public static IEnumerable<T> Parsed<T>(this IEnumerable<string?> texts)
        where T : class, ITextValue<T> => [];
}