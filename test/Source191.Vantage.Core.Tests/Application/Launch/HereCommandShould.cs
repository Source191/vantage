using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class HereCommandShould
{
    private const string Directory = @"C:\repos\Northwind.Orders";

    [Fact]
    public void StartInTheCurrentFolderOnItsBranchNamedAfterTheFolderByDefault()
    {
        LaunchPlan plan = HereCommand.Plan(Directory, [], isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Directory.Value.ShouldBe(Directory);
        plan.Name.Value.ShouldBe("Northwind.Orders");
        plan.Worktree.ShouldBeNull();
        plan.OpeningPrompt.ShouldBeNull();
        plan.Repository.ShouldBe(new RepositoryChoice("Northwind.Orders", new FolderPath(Directory), IsRepository: true));
    }

    [Fact]
    public void TakeANameAndAnOpeningPromptFromTheArguments()
    {
        LaunchPlan plan = HereCommand.Plan(Directory, ["fix-tests", "-p", "Fix the failing tests"], isRepository: _ => true).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Name.Value.ShouldBe("fix-tests");
        plan.OpeningPrompt.ShouldBe("Fix the failing tests");
    }

    [Theory]
    [InlineData("-d")]
    [InlineData("--description")]
    public void RefuseTheRemovedDescriptionOptionAsUnknown(string option)
    {
        LaunchPlanResult result = HereCommand.Plan(Directory, [option, "The flaky suite"], isRepository: _ => true);

        result.ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.UnknownOption(option)));
    }

    [Fact]
    public void WorkInAFolderThatIsNotARepository()
    {
        LaunchPlan plan = HereCommand.Plan(@"C:\notes", [], isRepository: _ => false).ShouldBeOfType<LaunchPlanResult.Planned>().Plan;

        plan.Repository.IsRepository.ShouldBeFalse();
        plan.Name.Value.ShouldBe("notes");
    }

    [Fact]
    public void RefuseBadArgumentsAndABadNameAsFacts()
    {
        HereCommand.Plan(Directory, ["--bogus"], isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.UnknownOption("--bogus")));
        HereCommand.Plan(Directory, ["-p"], isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.OptionNeedsValue("-p")));
        HereCommand.Plan(Directory, ["fix-123", "extra"], isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.UnexpectedArgument("extra")));
        HereCommand.Plan(Directory, ["a;b"], isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.InvalidName(NameProblem.UnsafeCharacters)));
        HereCommand.Plan(Directory, [""], isRepository: _ => true).ShouldBe(new LaunchPlanResult.Refused(new LaunchProblem.InvalidName(NameProblem.Missing)));
    }
}