using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

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

    /// <summary>
    /// The job id the CLI gives the session: its id's first eight characters; false when the id is shorter or those are
    /// not hex.
    /// </summary>
    public static bool TryParse(SessionId session, [MaybeNullWhen(false)] out JobId value) => TryParse(session.Value.Length >= Length ? session.Value[..Length] : null, out value);

    public override string ToString() => Value;

    private static bool IsJobId(string text) => text.Length == Length && text.All(char.IsAsciiHexDigit);
}