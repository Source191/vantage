using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

/// <summary>Why a typed problem report title is refused; <c>Wording</c> says it.</summary>
public enum ReportTitleProblem
{
    Missing,
    TooLong,
}

/// <summary>A typed title checked against the rules: the title, or the reason it is refused.</summary>
public abstract record ReportTitleValidation
{
    public sealed record Valid(ReportTitle Title) : ReportTitleValidation;

    public sealed record Refused(ReportTitleProblem Problem) : ReportTitleValidation;
}

/// <summary>
/// The title of a problem report, the one text in a report the user typed. It is never recorded in the diagnostic log.
/// At most 200 characters, so the title cannot crowd the diagnostics out of the page's address.
/// </summary>
public sealed record ReportTitle : ITextValue<ReportTitle>
{
    public const int MostCharacters = 200;

    private ReportTitle(string text)
    {
        if (HasProblem(text, out ReportTitleProblem problem))
        {
            throw new ArgumentException($"Not a report title: {problem}.", nameof(text));
        }

        Value = text;
    }

    public static ReportTitle From(string text) => new(text);

    public string Value { get; }

    /// <summary>Checks typed text, refused when blank or longer than <see cref="MostCharacters"/>.</summary>
    public static ReportTitleValidation Validate(string text)
    {
        return HasProblem(text, out ReportTitleProblem problem)
            ? new ReportTitleValidation.Refused(problem)
            : new ReportTitleValidation.Valid(ReportTitle.From(text));
    }

    public static bool TryParse(string? text, [MaybeNullWhen(false)] out ReportTitle value)
    {
        value = text is not null && !HasProblem(text, out _) ? ReportTitle.From(text) : null;
        return value is not null;
    }

    /// <summary>The two rules, in order: a title is not blank and not longer than <see cref="MostCharacters"/>.</summary>
    private static bool HasProblem(string text, out ReportTitleProblem problem)
    {
        (bool refused, problem) = text switch
        {
            _ when string.IsNullOrWhiteSpace(text) => (true, ReportTitleProblem.Missing),
            { Length: > MostCharacters } => (true, ReportTitleProblem.TooLong),
            _ => (false, default(ReportTitleProblem)),
        };

        return refused;
    }
}