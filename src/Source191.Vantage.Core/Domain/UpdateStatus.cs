namespace Source191.Vantage.Domain;

/// <summary>What Vantage knows about a newer release of itself: nothing to offer because the check is off, up to date, or one to update to.</summary>
public abstract record UpdateStatus
{
    /// <summary>The check is turned off, or this is a development build (a prerelease or an unknown version), which never checks.</summary>
    public sealed record Off : UpdateStatus;

    public sealed record UpToDate : UpdateStatus;

    public sealed record Available(ReleaseVersion Current, ReleaseVersion Newest) : UpdateStatus;
}