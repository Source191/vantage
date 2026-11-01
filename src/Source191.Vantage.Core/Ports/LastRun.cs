using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>Which released version last ran: none recorded (or a record that cannot be read), or the version.</summary>
public abstract record LastRun
{
    public sealed record Never : LastRun;

    public sealed record Ran(ReleaseVersion Version) : LastRun;
}