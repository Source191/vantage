namespace Source191.Vantage.Ports;

/// <summary>
/// What a package registry answered for a package's versions: the list, which may be empty (checked, nothing found),
/// or nothing learned (the request failed, timed out or answered something unreadable), which the caller never records.
/// </summary>
public abstract record VersionListing
{
    public sealed record Listed(IReadOnlyList<string> Versions) : VersionListing;

    public sealed record NothingLearned : VersionListing;
}