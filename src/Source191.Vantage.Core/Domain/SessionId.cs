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