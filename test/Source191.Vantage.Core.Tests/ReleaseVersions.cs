using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests;

/// <summary>Versions for the facts, written as text: a text that does not parse fails here, with the text named.</summary>
public static class ReleaseVersions
{
    public static ReleaseVersion Version(string text)
    {
        ReleaseVersion.TryParse(text, out ReleaseVersion? version).ShouldBeTrue($"\"{text}\" should read as a release version");

        return version;
    }
}