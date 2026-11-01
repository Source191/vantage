namespace Source191.Vantage.Domain;

/// <summary>The headings a changelog section's entries sit under, as Keep a Changelog names them; Internal entries are never shown.</summary>
public enum ChangeKind
{
    Added,
    Changed,
    Fixed,
    Removed,
    Security,
    Internal,
}