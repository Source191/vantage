namespace Source191.Vantage.Domain;

/// <summary>
/// The host a missing pull request was asked of: known once the origin remote has been read, or not read (no remote,
/// a host Vantage does not know, a lookup that failed), when the status line names both hosts' CLIs.
/// </summary>
public abstract record LookupHost
{
    public sealed record Known(PullRequestHost Host) : LookupHost;

    public sealed record NotRead : LookupHost;
}