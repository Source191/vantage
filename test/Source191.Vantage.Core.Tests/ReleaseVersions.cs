using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests;

/// <summary>Versions for the facts, written as text: a text that does not parse fails here, with the text named.</summary>
public static class ReleaseVersions
{
    public static ReleaseVersion Version(string text)
    {
        return ReleaseVersion.TryParse(text).ShouldNotBeNull($"\"{text}\" should read as a release version");
    }
}