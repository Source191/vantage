using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>Why a typed session name is refused; <c>Wording</c> says it.</summary>
public enum NameProblem
{
    Missing,
    UnsafeCharacters,
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
/// The name is a tab's title, and wt.exe splits its command line on a semicolon even inside a quoted argument, so a
/// name holds no semicolon. The double quote is refused with it, although it reaches the title intact; the Claude CLI
/// keeps a semicolon in a name.
/// </summary>
public sealed record SessionName : ITextValue<SessionName>
{
    private SessionName(string text)
    {
        if (ProblemWith(text) is NameProblem problem)
        {
            throw new ArgumentException($"'{text}' is not a session name: {problem}.", nameof(text));
        }

        Value = text;
    }

    public static SessionName From(string text) => new(text);

    public string Value { get; }

    /// <summary>Checks typed text: blank first, then the semicolon wt.exe would split the tab's title on, and the double quote.</summary>
    public static SessionNameValidation Validate(string text)
    {
        return ProblemWith(text) switch
        {
            NameProblem problem => new SessionNameValidation.Refused(problem),
            null => new SessionNameValidation.Valid(SessionName.From(text)),
        };
    }

    /// <summary>A listing name with a semicolon wt.exe would split a tab's title on, or a double quote, answers false rather than failing a resume.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out SessionName value)
    {
        value = text is not null && ProblemWith(text) is null ? SessionName.From(text) : null;
        return value is not null;
    }

    private static NameProblem? ProblemWith(string text)
    {
        return text switch
        {
            _ when string.IsNullOrWhiteSpace(text) => NameProblem.Missing,
            _ => null,
        };
    }
}