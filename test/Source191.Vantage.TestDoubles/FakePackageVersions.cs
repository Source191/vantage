using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>nuget.org's version list as a test sets it: a listing, or nothing learned for a failed or slow request, and how often it was asked.</summary>
public sealed class FakePackageVersions(VersionListing answer) : IPackageVersions
{
    public FakePackageVersions(IReadOnlyList<string> versions)
        : this(new VersionListing.Listed(versions))
    {
    }

    public VersionListing Answer { get; set; } = answer;

    public int Calls { get; private set; }

    public string? LastPackageId { get; private set; }

    public Task<VersionListing> ListAsync(string packageId, CancellationToken cancellationToken)
    {
        Calls++;
        LastPackageId = packageId;

        return Task.FromResult(Answer);
    }
}