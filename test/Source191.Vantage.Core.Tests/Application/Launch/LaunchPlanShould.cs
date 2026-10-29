using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class LaunchPlanShould
{
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", FolderPath.From(@"C:\repos\Northwind.Orders"), IsRepository: true);
    private static readonly RepositoryChoice NoRepository = new("repos (no repository)", FolderPath.From(@"C:\repos"), IsRepository: false);

    private static readonly FolderPath WorktreeRoot = FolderPath.From(@"C:\Users\me\source\worktrees");

    private static readonly LaunchRequest Default = new(
        Repository,
        NameText: "Fix #123: flaky tests!",
        Mode: LaunchMode.CurrentBranch,
        Base: WorktreeBase.RemoteDefault,
        OpeningPromptText: string.Empty);

    [Fact]
    public void StartInTheRepositoryOnTheCurrentBranchByDefault()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default, WorktreeRoot, folderExists: _ => false).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Directory.Value.ShouldBe(@"C:\repos\Northwind.Orders");
        plan.Worktree.ShouldBeNull();
        plan.Name.Value.ShouldBe("Fix #123: flaky tests!");
    }

    [Fact]
    public void DropABlankOpeningPromptSoTheSessionStartsIdle()
    {
        LaunchPlanBuilder.Build(Default, WorktreeRoot, _ => false).ShouldBeOfType<LaunchPlanResult.Planned>().Plan.OpeningPrompt.ShouldBeNull();
        LaunchPlanBuilder.Build(Default with { OpeningPromptText = "Fix the test" }, WorktreeRoot, _ => false).ShouldBeOfType<LaunchPlanResult.Planned>().Plan.OpeningPrompt.ShouldBe("Fix the test");
    }

    [Fact]
    public void PlaceANewWorktreeUnderTheWorktreeRoot()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default with { Mode = LaunchMode.NewWorktree }, WorktreeRoot, _ => false).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Directory.Value.ShouldBe(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123-flaky-tests");
        plan.Worktree.ShouldNotBeNull();
        plan.Worktree.Branch.ShouldBe(BranchName.From("fix-123-flaky-tests"));
        plan.ReuseExistingWorktree.ShouldBeFalse();
        plan.Base.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public void ReuseAnExistingFolderUnderTheWorktreeRoot()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default with { Mode = LaunchMode.NewWorktree }, WorktreeRoot, folderExists: path => path.EndsWith("flaky-tests", StringComparison.Ordinal)).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.ReuseExistingWorktree.ShouldBeTrue();
        plan.Directory.Value.ShouldBe(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123-flaky-tests");
    }

    [Fact]
    public void RefuseAWorktreeWhenTheNameHasNoLettersOrDigits()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = "!!!", Mode = LaunchMode.NewWorktree }, WorktreeRoot, _ => false);

        result.ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsLetters()));
    }

    [Fact]
    public void RefuseAWorktreeForTheNoRepositoryChoice()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { Repository = NoRepository, Mode = LaunchMode.NewWorktree }, WorktreeRoot, _ => false);

        result.ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.WorktreeNeedsRepository()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReportAnInvalidName(string name)
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = name }, WorktreeRoot, _ => false);

        result.ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.InvalidName(NameProblem.Missing)));
    }

    [Theory]
    [InlineData("- fix the tests")]
    [InlineData("--dangerously-skip-permissions")]
    [InlineData("  -p something")]
    public void RefuseAnOpeningPromptThatStartsWithADash(string prompt)
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { OpeningPromptText = prompt }, WorktreeRoot, _ => false);

        result.ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.PromptLooksLikeAnOption()));
    }

    [Fact]
    public void LaunchAnOpeningPromptWithADashInsideIt()
    {
        LaunchPlanBuilder.Build(Default with { OpeningPromptText = "Fix the tests - all of them" }, WorktreeRoot, _ => false)
            .ShouldBeOfType<LaunchPlanResult.Planned>().Plan.OpeningPrompt.ShouldBe("Fix the tests - all of them");
    }

    [Fact]
    public void LaunchANameWithASemicolon()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = "fix a;b" }, WorktreeRoot, _ => false);

        result.ShouldBeOfType<LaunchPlanResult.Planned>().Plan.Name.Value.ShouldBe("fix a;b");
    }
}