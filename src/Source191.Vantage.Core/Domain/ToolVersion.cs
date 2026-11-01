namespace Source191.Vantage.Domain;

/// <summary>The version Vantage reports for itself: the package's informational version without the commit, and a local build's timestamp cut to just "local".</summary>
public sealed record ToolVersion(string Text)
{
    private const string Unknown = "unknown";
    private const string LocalPrerelease = "-local.";

    public static ToolVersion FromInformational(string informational)
    {
        ArgumentNullException.ThrowIfNull(informational);
        string withoutCommit = informational.Split('+')[0].Trim();
        if (withoutCommit.Length == 0)
        {
            return new ToolVersion(Unknown);
        }

        int local = withoutCommit.IndexOf(LocalPrerelease, StringComparison.Ordinal);

        return new ToolVersion(local < 0 ? withoutCommit : withoutCommit[..(local + LocalPrerelease.Length - 1)]);
    }
}