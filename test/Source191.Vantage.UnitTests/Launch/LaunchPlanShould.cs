using Source191.Vantage.Launch;

namespace Source191.Vantage.UnitTests.Launch;

public sealed class LaunchPlanShould
{
    private static readonly RepositoryChoice Repository = new("Northwind.Orders", @"C:\repos\Northwind.Orders", IsRepository: true);
    private static readonly RepositoryChoice NoRepository = new("repos (no repository)", @"C:\repos", IsRepository: false);

    private static readonly LaunchRequest Default = new(
        Repository,
        NameText: "Fix #123: flaky tests!",
        DescriptionText: "  Stabilise the retry test  ",
        Mode: LaunchMode.CurrentBranch,
        Base: WorktreeBase.RemoteDefault,
        OpeningPromptText: string.Empty);

    [Fact]
    public void StartInTheRepositoryOnTheCurrentBranchByDefault()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default, folderExists: _ => false).Plan!;

        plan.Directory.ShouldBe(@"C:\repos\Northwind.Orders");
        plan.Worktree.ShouldBeNull();
        plan.Name.Text.ShouldBe("Fix #123: flaky tests!");
    }

    [Fact]
    public void TrimTheDescriptionAndDropItWhenBlank()
    {
        LaunchPlanBuilder.Build(Default, _ => false).Plan!.Description.ShouldBe("Stabilise the retry test");
        LaunchPlanBuilder.Build(Default with { DescriptionText = "   " }, _ => false).Plan!.Description.ShouldBeNull();
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

        plan.Directory.ShouldBe(@"C:\repos\Northwind.Orders-fix-123-flaky-tests");
        plan.Worktree.ShouldNotBeNull();
        plan.Worktree.Branch.ShouldBe("fix-123-flaky-tests");
        plan.ReuseExistingWorktree.ShouldBeFalse();
        plan.Base.ShouldBe(WorktreeBase.RemoteDefault);
    }

    [Fact]
    public void ReuseAnExistingWorktreeFolderInsteadOfCreatingOne()
    {
        LaunchPlan plan = LaunchPlanBuilder.Build(Default with { Mode = LaunchMode.NewWorktree }, folderExists: path => path.EndsWith("flaky-tests", StringComparison.Ordinal)).Plan!;

        plan.ReuseExistingWorktree.ShouldBeTrue();
        plan.Directory.ShouldBe(@"C:\repos\Northwind.Orders-fix-123-flaky-tests");
    }

    [Fact]
    public void RefuseAWorktreeWhenTheNameHasNoLettersOrDigits()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = "!!!", Mode = LaunchMode.NewWorktree }, _ => false);

        result.Plan.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem.ShouldContain("letters or digits");
    }

    [Fact]
    public void RefuseAWorktreeForTheNoRepositoryChoice()
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { Repository = NoRepository, Mode = LaunchMode.NewWorktree }, _ => false);

        result.Plan.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem.ShouldContain("repository");
    }

    [Theory]
    [InlineData("", "required")]
    [InlineData("a; b", "semicolon")]
    public void ReportAnInvalidName(string name, string expected)
    {
        LaunchPlanResult result = LaunchPlanBuilder.Build(Default with { NameText = name }, _ => false);

        result.Plan.ShouldBeNull();
        result.Problem.ShouldNotBeNull();
        result.Problem.ShouldContain(expected);
    }
}