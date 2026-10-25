using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>nuget.org's version list as a test sets it: an answer, or null for a failed or slow request, and how often it was asked.</summary>
public sealed class FakePackageVersions(IReadOnlyList<string>? answer) : IPackageVersions
{
    public IReadOnlyList<string>? Answer { get; set; } = answer;

    public int Calls { get; private set; }

    public string? LastPackageId { get; private set; }

    public Task<IReadOnlyList<string>?> ListAsync(string packageId, CancellationToken cancellationToken)
    {
        Calls++;
        LastPackageId = packageId;

        return Task.FromResult(Answer);
    }
}