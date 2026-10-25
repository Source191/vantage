using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

/// <summary>
/// The part of a worktree's folder and branch that comes from a session name: lower case, each run of anything but
/// letters and digits a hyphen, and no hyphen at either end. Derived rather than parsed, so a name with no letters or
/// digits has none.
/// </summary>
public sealed partial record WorktreeSlug : ITextValue<WorktreeSlug>
{
    /// <summary>The slug of text known to hold letters or digits; throws otherwise.</summary>
    public WorktreeSlug(string text)
    {
        string slug = Slugged(text);
        if (slug.Length == 0)
        {
            throw new ArgumentException($"'{text}' has no letters or digits to make a worktree slug from.", nameof(text));
        }

        Value = slug;
    }

    public string Value { get; }

    /// <summary>The slug of a name being typed or planned; false when it has no letters or digits.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out WorktreeSlug value)
    {
        value = text is not null && Slugged(text).Length > 0 ? new WorktreeSlug(text) : null;
        return value is not null;
    }

    private static string Slugged(string text) => NonAlphanumericRuns().Replace(text.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRuns();
}