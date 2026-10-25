using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

public sealed partial record WorktreeSlug : ITextValue<WorktreeSlug>
{
    public WorktreeSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryParse(string? text, [MaybeNullWhen(false)] out WorktreeSlug value)
    {
        value = null;
        return false;
    }

    public static WorktreeSlug? From(string text)
    {
        string slug = NonAlphanumericRuns().Replace(text.ToLowerInvariant(), "-").Trim('-');

        return slug.Length == 0 ? null : new WorktreeSlug(slug);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRuns();
}