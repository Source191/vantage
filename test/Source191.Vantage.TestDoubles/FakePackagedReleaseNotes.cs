using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>The notes inside the installed tool as a test sets them: the changelog's text, or nothing learned when it is missing.</summary>
public sealed class FakePackagedReleaseNotes(PackageNotes answer) : IPackagedReleaseNotes
{
    public FakePackagedReleaseNotes(string text)
        : this(new PackageNotes.Read(text))
    {
    }

    public int Reads { get; private set; }

    public Task<PackageNotes> ReadAsync()
    {
        Reads++;

        return Task.FromResult(answer);
    }
}