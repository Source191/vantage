using System.Text.RegularExpressions;

namespace Source191.Vantage.Domain;

public sealed partial record WorktreeSlug
{
    private WorktreeSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static WorktreeSlug? From(string text)
    {
        string slug = NonAlphanumericRuns().Replace(text.ToLowerInvariant(), "-").Trim('-');

        return slug.Length == 0 ? null : new WorktreeSlug(slug);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRuns();
}