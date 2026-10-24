namespace Source191.Vantage.Domain;

public sealed record SessionName
{
    private SessionName(string text)
    {
        Text = text;
    }

    public string Text { get; }

    public WorktreeSlug? Slug => WorktreeSlug.From(Text);

    /// <summary>A listing name the CLI would refuse (quotes, semicolons) reads as null rather than failing a resume.</summary>
    public static SessionName? TryParse(string text)
    {
        try
        {
            return Parse(text);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public static SessionName Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("A session name is required.", nameof(text));
        }

        if (text.Contains(';') || text.Contains('"'))
        {
            throw new ArgumentException(
                "A session name cannot contain a semicolon or a double quote, because Windows Terminal cannot pass them safely.",
                nameof(text));
        }

        return new SessionName(text);
    }
}