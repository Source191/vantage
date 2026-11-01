namespace Source191.Vantage.Domain;

public sealed record RowFilter(string Text)
{
    public static readonly RowFilter None = new(string.Empty);

    public bool Matches(IReadOnlyList<string> fields)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return true;
        }

        return fields.Any(field => field.Contains(Text, StringComparison.OrdinalIgnoreCase));
    }
}