using System.Diagnostics;

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

public sealed record SessionName
{
    private SessionName(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public WorktreeSlug? Slug => WorktreeSlug.From(Text);

    /// <summary>Checks typed text: blank first, then the semicolon and double quote Windows Terminal cannot pass safely.</summary>
    public static SessionNameValidation Validate(string text)
    {
        return text switch
        {
            _ when string.IsNullOrWhiteSpace(text) => new SessionNameValidation.Refused(NameProblem.Missing),
            _ when text.Contains(';') || text.Contains('"') => new SessionNameValidation.Refused(NameProblem.UnsafeCharacters),
            _ => new SessionNameValidation.Valid(new SessionName(text)),
        };
    }

    /// <summary>A listing name the CLI would refuse (quotes, semicolons) reads as null rather than failing a resume.</summary>
    public static SessionName? TryParse(string text) => Validate(text) is SessionNameValidation.Valid valid ? valid.Name : null;

    /// <summary>For text already known to be a valid name; typed text goes through <see cref="Validate"/>.</summary>
    public static SessionName Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        return Validate(text) switch
        {
            SessionNameValidation.Valid valid => valid.Name,
            SessionNameValidation.Refused refused => throw new ArgumentException($"'{text}' is not a session name: {refused.Problem}.", nameof(text)),
            _ => throw new UnreachableException(),
        };
    }
}