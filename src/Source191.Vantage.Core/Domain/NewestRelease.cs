namespace Source191.Vantage.Domain;

/// <summary>
/// The newest release Vantage knows of: none yet, one found on nuget.org or in the update-check record, or recorded
/// text that is not a version. A named case stands where a null would, because <see cref="UpdateCheckRule.Offer"/>
/// offers only a <see cref="Found"/> one.
/// </summary>
public abstract record NewestRelease
{
    /// <summary>Recorded text: <see cref="Found"/> when it reads as a version, <see cref="Unreadable"/> when it does not.</summary>
    public static NewestRelease Recorded(string text) => ReleaseVersion.TryParse(text, out ReleaseVersion? version) ? new Found(version) : new Unreadable(text);

    public sealed record None : NewestRelease;

    public sealed record Found(ReleaseVersion Version) : NewestRelease;

    /// <summary>Recorded text that is not a version, reached only by a hand-edited record; the Config tab shows it as it is.</summary>
    public sealed record Unreadable(string Text) : NewestRelease;
}