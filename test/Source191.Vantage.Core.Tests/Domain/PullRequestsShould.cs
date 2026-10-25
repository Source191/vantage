using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class PullRequestsShould
{
    private static readonly DateTimeOffset Monday = new(2026, 10, 19, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(PullRequestState.Open)]
    [InlineData(PullRequestState.Draft)]
    [InlineData(PullRequestState.Active)]
    public void PreferTheOpenOneCountingDraftsAndActive(PullRequestState open)
    {
        PullRequest older = GitHub(7, open, Monday);
        PullRequest newer = GitHub(9, PullRequestState.Merged, Monday.AddDays(2));

        PullRequests.ForBranch([newer, older]).ShouldBe(older);
    }

    [Fact]
    public void OtherwiseTakeTheMostRecent()
    {
        PullRequest closed = GitHub(7, PullRequestState.Closed, Monday);
        PullRequest merged = GitHub(9, PullRequestState.Merged, Monday.AddDays(2));
        PullRequest abandoned = GitHub(8, PullRequestState.Abandoned, Monday.AddDays(1));

        PullRequests.ForBranch([closed, merged, abandoned]).ShouldBe(merged);
    }

    [Fact]
    public void AnswerNothingForNoPullRequests()
    {
        PullRequests.ForBranch([]).ShouldBeNull();
    }

    [Theory]
    [InlineData(PullRequestState.Open, true)]
    [InlineData(PullRequestState.Draft, true)]
    [InlineData(PullRequestState.Active, true)]
    [InlineData(PullRequestState.Merged, false)]
    [InlineData(PullRequestState.Closed, false)]
    [InlineData(PullRequestState.Completed, false)]
    [InlineData(PullRequestState.Abandoned, false)]
    public void KnowWhichStatesAreOpen(PullRequestState state, bool open)
    {
        state.IsOpen().ShouldBe(open);
    }

    private static PullRequest GitHub(int number, PullRequestState state, DateTimeOffset createdAt) =>
        new(PullRequestHost.GitHub, number, new Uri($"https://github.com/northwind/Northwind.Orders/pull/{number}"), state, createdAt);
}
