namespace Source191.Vantage.Ports;

/// <summary>
/// What was read of a release's notes: the text as written (from the package's nuspec, or the changelog inside the
/// installed tool), or nothing learned (the request failed, timed out, the file is missing or the reply is unreadable),
/// which the caller treats as no notes and never explains.
/// </summary>
public abstract record PackageNotes
{
    public sealed record Read(string Text) : PackageNotes;

    public sealed record NothingLearned : PackageNotes;
}