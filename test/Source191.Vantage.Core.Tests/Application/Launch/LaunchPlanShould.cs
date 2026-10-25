using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class LaunchPlanShould
{
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", new FolderPath(@"C:\repos\Northwind.Orders"), IsRepository: true);
    private static readonly RepositoryChoice NoRepository = new("repos (no repository)", new FolderPath(@"C:\repos"), IsRepository: false);

    private static readonly LaunchRequest Default = new(
        Repository,
        NameText: "Fix #123: flaky tests!",
        Mode: LaunchMode.CurrentBranch,
        Base: WorktreeBase.RemoteDefault,
        OpeningPromptText: string.Empty);

    [Fact]
    public void StartInTheRepositoryOnTheCurrentBranchByDefault()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default, folderExists: _ => false).Plan!;

        plan.Directory.Value.ShouldBe(@"C:\repos\Northwind.Orders");
        plan.Worktree.ShouldBeNull();
        plan.Name.Value.ShouldBe("Fix #123: flaky tests!");
    }

    [Fact]
    public void DropABlankOpeningPromptSoTheSessionStartsIdle()
    {
        LaunchPlanBuilder.Build(Default, _ => false).Plan!.OpeningPrompt.ShouldBeNull();
        LaunchPlanBuilder.Build(Default with { OpeningPromptText = "Fix the test" }, _ => false).Plan!.OpeningPrompt.ShouldBe("Fix the test");
    }

    [Fact]
    public void PlaceANewWorktreeBesideTheRepositoryOnTheSlugBranch()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default with { Mode = LaunchMode.NewWorktree }, _ => false).Plan!;

        plan.Directory.Value.ShouldBe(@"C:\repos\Northwind.Orders-fix-123-flaky-tests");
        plan.Worktree.ShouldNotBeNull();
        plan.Worktree.Branch.ShouldBe(new BranchName("fix-123-flaky-tests"));
        plan.ReuseExistingWorktree.ShouldBeFalse();
        plan.Base.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public void ReuseAnExistingWorktreeFolderInsteadOfCreatingOne()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default with { Mode = LaunchMode.NewWorktree }, folderExists: path => path.EndsWith("flaky-tests", StringComparison.Ordinal)).Plan!;

        plan.ReuseExistingWorktree.ShouldBeTrue();
        plan.Directory.Value.ShouldBe(@"C:\repos\Northwind.Orders-fix-123-flaky-tests");
    }

    [Fact]
    public void RefuseAWorktreeWhenTheNameHasNoLettersOrDigits()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = "!!!", Mode = LaunchMode.NewWorktree }, _ => false);

        result.Plan.ShouldBeNull();
        result.Problem.ShouldBe(new LaunchProblem.WorktreeNeedsLetters());
    }

    [Fact]
    public void RefuseAWorktreeForTheNoRepositoryChoice()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { Repository = NoRepository, Mode = LaunchMode.NewWorktree }, _ => false);

        result.Plan.ShouldBeNull();
        result.Problem.ShouldBe(new LaunchProblem.WorktreeNeedsRepository());
    }

    [Theory]
    [InlineData("", NameProblem.Missing)]
    [InlineData("a; b", NameProblem.UnsafeCharacters)]
    public void ReportAnInvalidName(string name, NameProblem expected)
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = name }, _ => false);

        result.Plan.ShouldBeNull();
        result.Problem.ShouldBe(new LaunchProblem.InvalidName(expected));
    }
}