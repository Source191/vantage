using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>Why a typed session name is refused; <c>Wording</c> says it.</summary>
public enum NameProblem
{
    Missing,
}

/// <summary>A typed name checked against the rules: the name, or the reason it is refused.</summary>
public abstract record SessionNameValidation
{
    public sealed record Valid(SessionName Name) : SessionNameValidation;

    public sealed record Refused(NameProblem Problem) : SessionNameValidation;
}

/// <summary>
/// A session's name. Three doors share one rule: the constructor for text already known to be a valid name,
/// <see cref="Validate"/> for typed text, which says why it refuses, and <see cref="TryParse"/> for a listing's name.
/// Only a blank name is refused: a semicolon or a double quote is kept as typed, and the terminal adapter escapes what
/// wt.exe would otherwise split a tab's title on.
/// </summary>
public sealed record SessionName : ITextValue<SessionName>
{
    private SessionName(string text)
    {
        if (!IsName(text))
        {
            throw new ArgumentException($"'{text}' is not a session name: {NameProblem.Missing}.", nameof(text));
        }

        Value = text;
    }

    public static SessionName From(string text) => new(text);

    public string Value { get; }

    /// <summary>Checks typed text, which is refused only when blank.</summary>
    public static SessionNameValidation Validate(string text)
    {
        return IsName(text)
            ? new SessionNameValidation.Valid(SessionName.From(text))
            : new SessionNameValidation.Refused(NameProblem.Missing);
    }

    /// <summary>A blank or missing listing name answers false rather than failing a resume.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out SessionName value)
    {
        value = text is not null && IsName(text) ? SessionName.From(text) : null;
        return value is not null;
    }

    /// <summary>The one rule: a name is not blank.</summary>
    private static bool IsName(string text) => !string.IsNullOrWhiteSpace(text);
}