using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class HeadShould
{
    [Fact]
    public void KeepTheBranchOnABranch()
    {
        Head head = new Head.OnBranch(BranchName.From("fix-123"));

        head.ShouldBeOfType<Head.OnBranch>().Branch.ShouldBe(BranchName.From("fix-123"));
    }

    [Fact]
    public void TellADetachedHeadFromOneGitDidNotName()
    {
        Head detached = new Head.Detached();
        Head unknown = new Head.Unknown();

        detached.ShouldNotBe(unknown);
        detached.ShouldBe(new Head.Detached());
        unknown.ShouldBe(new Head.Unknown());
    }
}