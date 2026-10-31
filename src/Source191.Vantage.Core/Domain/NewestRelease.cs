namespace Source191.Vantage.Domain;

/// <summary>
/// The newest release Vantage knows of: none yet, or one found on nuget.org or in the update-check record. A named
/// case stands where a null would, because <see cref="UpdateCheckRule.Offer"/> acts differently on each.
/// </summary>
public abstract record NewestRelease
{
    /// <summary>Recorded text: <see cref="Found"/> when it reads as a version, <see cref="None"/> when it does not.</summary>
    public static NewestRelease Recorded(string text) => ReleaseVersion.TryParse(text) is { } version ? new Found(version) : new None();

    public sealed record None : NewestRelease;

    public sealed record Found(ReleaseVersion Version) : NewestRelease;
}