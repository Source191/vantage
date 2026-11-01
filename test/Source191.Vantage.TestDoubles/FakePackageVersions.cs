using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>
/// nuget.org as a test sets it: a version listing, or nothing learned for a failed or slow request, and how often it was
/// asked; release notes per version (nothing learned for a version without notes), with every version they were asked for.
/// </summary>
public sealed class FakePackageVersions(VersionListing answer) : IPackageVersions
{
    public FakePackageVersions(IReadOnlyList<string> versions)
        : this(new VersionListing.Listed(versions))
    {
    }

    public VersionListing Answer { get; set; } = answer;

    public int Calls { get; private set; }

    public string? LastPackageId { get; private set; }

    /// <summary>The notes text each version answers with.</summary>
    public Dictionary<string, string> Notes { get; } = [];

    /// <summary>The versions whose notes were asked for, in order.</summary>
    public List<ReleaseVersion> NotesAskedFor { get; } = [];

    public Task<VersionListing> ListAsync(string packageId, CancellationToken cancellationToken)
    {
        Calls++;
        LastPackageId = packageId;

        return Task.FromResult(Answer);
    }

    public Task<PackageNotes> NotesAsync(string packageId, ReleaseVersion version, CancellationToken cancellationToken)
    {
        NotesAskedFor.Add(version);

        return Task.FromResult<PackageNotes>(Notes.TryGetValue(version.ToString(), out string? text) ? new PackageNotes.Read(text) : new PackageNotes.NothingLearned());
    }
}