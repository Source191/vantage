using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class PullRequestLookupShould
{
    [Fact]
    public void MissWithTheHostNotReadUnlessGiven()
    {
        new PullRequestLookup.Missing(PullRequestGap.NoRemote).Host.ShouldBe(new LookupHost.NotRead());
        new PullRequestLookup.Missing(PullRequestGap.NotSignedIn, new LookupHost.Known(PullRequestHost.GitHub)).Host
            .ShouldBeOfType<LookupHost.Known>().Host.ShouldBe(PullRequestHost.GitHub);
    }
}