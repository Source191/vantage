using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>
/// A Claude Code session's id, in the CLI's shape: ASCII letters, digits and hyphens, a letter or digit first. The CLI's
/// ids are lowercase UUIDs; the rule is wider so a future format with the same characters still works, and narrow
/// enough that an id can neither be read as an option of <c>claude --resume</c> nor hold a path separator.
/// </summary>
public sealed record SessionId : ITextValue<SessionId>
{
    private SessionId(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (!IsSessionId(text))
        {
            throw new ArgumentException($"{text} is not a session id: letters, digits and hyphens, a letter or digit first.", nameof(text));
        }

        Value = text;
    }

    public static SessionId From(string text) => new(text);

    public string Value { get; }

    /// <summary>An id from outside Vantage (the CLI's listing, a state file); false when it is not in the CLI's shape.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out SessionId value)
    {
        value = text is not null && IsSessionId(text) ? SessionId.From(text) : null;
        return value is not null;
    }

    public override string ToString() => Value;

    private static bool IsSessionId(string text) => text.Length > 0 && char.IsAsciiLetterOrDigit(text[0]) && text.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');
}

/// <summary>A background job's id: the first eight characters of its session id, in hex, as the CLI lists it.</summary>
public sealed record JobId : ITextValue<JobId>
{
    private const int Length = 8;

    private JobId(string text)
    {
        if (!IsJobId(text))
        {
            throw new ArgumentException($"{text} is not a job id: eight hex characters.", nameof(text));
        }

        Value = text;
    }

    public static JobId From(string text) => new(text);

    public string Value { get; }

    /// <summary>A job id from outside Vantage (the CLI's listing or its answer to a start); false when it is not one.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out JobId value)
    {
        value = text is not null && IsJobId(text) ? JobId.From(text) : null;
        return value is not null;
    }

    /// <summary>The job id the CLI gives the session: its id's first eight characters; false when those are not hex.</summary>
    public static bool TryParse(SessionId session, [MaybeNullWhen(false)] out JobId value) => TryParse(session.Value.Length >= Length ? session.Value[..Length] : null, out value);

    public override string ToString() => Value;

    private static bool IsJobId(string text) => text.Length == Length && text.All(char.IsAsciiHexDigit);
}

/// <summary>A git branch name: not blank and without whitespace, which git refuses in a branch name.</summary>
public sealed record BranchName : ITextValue<BranchName>
{
    private BranchName(string text)
    {
        if (!IsBranchName(text))
        {
            throw new ArgumentException($"'{text}' is not a branch name: it is blank or holds whitespace.", nameof(text));
        }

        Value = text;
    }

    public static BranchName From(string text) => new(text);

    public string Value { get; }

    /// <summary>A branch name from outside Vantage (git's output, a state file); false when it is not one.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out BranchName value)
    {
        value = text is not null && IsBranchName(text) ? BranchName.From(text) : null;
        return value is not null;
    }

    public override string ToString() => Value;

    private static bool IsBranchName(string text) => text.Length > 0 && !text.Any(char.IsWhiteSpace);
}