using System.Diagnostics.CodeAnalysis;

namespace Source191.Vantage.Domain;

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