using System.Diagnostics.CodeAnalysis;

namespace Source191.Torni.Domain;

/// <summary>
/// A git branch name, by a minimal rule: not blank and without whitespace. Branch text comes from the console's
/// own slugs and git's own output, so the rule only guards against a blank or a split name; git's
/// <c>check-ref-format</c> refuses far more.
/// </summary>
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

    /// <summary>A branch name from outside the program (git's output, a state file); false when it is not one.</summary>
    public static bool TryParse(string? text, [MaybeNullWhen(false)] out BranchName value)
    {
        value = text is not null && IsBranchName(text) ? BranchName.From(text) : null;
        return value is not null;
    }

    public override string ToString() => Value;

    private static bool IsBranchName(string text) => text.Length > 0 && !text.Any(char.IsWhiteSpace);
}