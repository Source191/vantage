using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>
/// The one home of a release's GitHub page, built from the version alone: no text from the release notes ever reaches
/// a URL.
/// </summary>
public static class ReleasePage
{
    public static Uri For(ReleaseVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new Uri($"https://github.com/Source191/vantage/releases/tag/v{version}");
    }
}