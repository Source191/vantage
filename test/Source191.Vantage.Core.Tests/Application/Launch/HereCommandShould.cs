using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class HereCommandShould
{
    private const string Directory = @"C:\repos\Northwind.Orders";

    [Fact]
    public void StartInTheCurrentFolderOnItsBranchNamedAfterTheFolderByDefault()
    {
        LaunchPlan plan = HereCommand.Plan(Directory, new HereName.FolderName(), prompt: string.Empty, isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Directory.Value.ShouldBe(Directory);
        plan.Name.Value.ShouldBe("Northwind.Orders");
        plan.Worktree.ShouldBeNull();
        plan.OpeningPrompt.ShouldBe(new OpeningPrompt.None());
        plan.Repository.ShouldBe(new RepositoryChoice("Northwind.Orders", FolderPath.From(Directory), IsRepository: true));
    }

    [Fact]
    public void TakeANameAndAnOpeningPrompt()
    {
        LaunchPlan plan = HereCommand.Plan(Directory, new HereName.Typed("fix-tests"), "Fix the failing tests", isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Name.Value.ShouldBe("fix-tests");
        plan.OpeningPrompt.ShouldBe(new OpeningPrompt.Text("Fix the failing tests"));
    }

    [Fact]
    public void WorkInAFolderThatIsNotARepository()
    {
        LaunchPlan plan = HereCommand.Plan(@"C:\notes", new HereName.FolderName(), prompt: string.Empty, isRepository: _ => false).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Repository.IsRepository.ShouldBeFalse();
        plan.Name.Value.ShouldBe("notes");
    }

    [Fact]
    public void PlanWithAnEmptyOrATypedPrompt()
    {
        HereCommand.Plan(Directory, new HereName.Typed("fix-tests"), string.Empty, isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan.OpeningPrompt.ShouldBe(new OpeningPrompt.None());
        HereCommand.Plan(Directory, new HereName.Typed("fix-tests"), " Fix it ", isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan.OpeningPrompt.ShouldBe(new OpeningPrompt.Text("Fix it"));
    }

    [Fact]
    public void RefuseABadNameAsAFact()
    {
        HereCommand.Plan(Directory, new HereName.Typed(""), prompt: string.Empty, isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.InvalidName(NameProblem.Missing)));
    }

    [Fact]
    public void RefuseADashLedPromptAsAFact()
    {
        HereCommand.Plan(Directory, new HereName.Typed("fix-tests"), "-fix the tests", isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.PromptLooksLikeAnOption()));
    }

    [Fact]
    public void PlanANameWithASemicolonOrDoubleQuote()
    {
        HereCommand.Plan(Directory, new HereName.Typed("a;b"), prompt: string.Empty, isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan.Name.Value.ShouldBe("a;b");
        HereCommand.Plan(Directory, new HereName.Typed("say \"hi\""), prompt: string.Empty, isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan.Name.Value.ShouldBe("say \"hi\"");
    }
}