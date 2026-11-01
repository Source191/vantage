using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The last update check: never made (or a record that cannot be read), or when Vantage last asked for its newest release, what it found and that release's notes.</summary>
public abstract record UpdateCheckRecord
{
    public sealed record Never : UpdateCheckRecord;

    /// <summary>A check made; <paramref name="NewestNotes"/> is <see cref="ReleaseNotes.None"/> when the notes could not be read or were not asked for, because the newest release is not newer.</summary>
    public sealed record Checked(DateTimeOffset CheckedAt, NewestRelease Newest, ReleaseNotes NewestNotes) : UpdateCheckRecord
    {
        /// <summary>A check with no notes, which is how every record written before the notes were kept reads.</summary>
        public Checked(DateTimeOffset checkedAt, NewestRelease newest)
            : this(checkedAt, newest, ReleaseNotes.None)
        {
        }
    }
}