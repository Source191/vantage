using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The last update check: never made (or a record that cannot be read), or when Vantage last asked for its newest release and what it found.</summary>
public abstract record UpdateCheckRecord
{
    public sealed record Never : UpdateCheckRecord;

    public sealed record Checked(DateTimeOffset CheckedAt, NewestRelease Newest) : UpdateCheckRecord;
}