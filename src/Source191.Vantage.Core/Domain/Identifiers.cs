using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>A Claude Code session's id. The CLI does not document its format, so the only rule is that it is not blank.</summary>
public sealed record SessionId : ITextValue<SessionId>
{
    public SessionId(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Value = text;
    }

    public string Value { get; }

    /// <summary>An id from outside Vantage (the CLI's listing, a state file); null when it is blank.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out SessionId value)
    {
        value = null;
        return false;
    }

    public static SessionId? TryParse(string? text) => string.IsNullOrWhiteSpace(text) ? null : new SessionId(text);

    public override string ToString() => Value;
}

/// <summary>A background job's id: the first eight characters of its session id, in hex, as the CLI lists it.</summary>
public sealed record JobId : ITextValue<JobId>
{
    private const int Length = 8;

    public JobId(string text)
    {
        if (!IsJobId(text))
        {
            throw new ArgumentException($"{text} is not a job id: eight hex characters.", nameof(text));
        }

        Value = text;
    }

    public string Value { get; }

    public static bool TryParse(string? text, [MaybeNullWhen(false)] out JobId value)
    {
        value = null;
        return false;
    }

    public static bool TryParse(SessionId session, [MaybeNullWhen(false)] out JobId value)
    {
        value = null;
        return false;
    }

    public static JobId? TryParse(string? text) => text is not null && IsJobId(text) ? new JobId(text) : null;

    /// <summary>The job id the CLI gives the session: its id's first eight characters; null when those are not hex.</summary>
    public static JobId? Of(SessionId session) => TryParse(session.Value.Length >= Length ? session.Value[..Length] : null);

    public override string ToString() => Value;

    private static bool IsJobId(string text) => text.Length == Length && text.All(char.IsAsciiHexDigit);
}

/// <summary>A git branch name: not blank and without whitespace, which git refuses in a branch name.</summary>
public sealed record BranchName : ITextValue<BranchName>
{
    public BranchName(string text)
    {
        if (!IsBranchName(text))
        {
            throw new ArgumentException($"'{text}' is not a branch name: it is blank or holds whitespace.", nameof(text));
        }

        Value = text;
    }

    public string Value { get; }

    /// <summary>A branch name from outside Vantage (git's output, a state file); null when it is not one.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out BranchName value)
    {
        value = null;
        return false;
    }

    public static BranchName? TryParse(string? text) => text is not null && IsBranchName(text) ? new BranchName(text) : null;

    public override string ToString() => Value;

    private static bool IsBranchName(string text) => text.Length > 0 && !text.Any(char.IsWhiteSpace);
}