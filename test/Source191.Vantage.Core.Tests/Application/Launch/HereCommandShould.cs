using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Launch;

public sealed class HereCommandShould
{
    private const string Directory = @"C:\repos\Northwind.Orders";

    [Fact]
    public void StartInTheCurrentFolderOnItsBranchNamedAfterTheFolderByDefault()
    {
        LaunchPlanResult result = HereCommand.Plan(Directory, [], isRepository: _ => true);

        result.Problem.ShouldBeNull();
        result.Plan.ShouldNotBeNull();
        result.Plan.Directory.Value.ShouldBe(Directory);
        result.Plan.Name.Text.ShouldBe("Northwind.Orders");
        result.Plan.Worktree.ShouldBeNull();
        result.Plan.Description.ShouldBeNull();
        result.Plan.OpeningPrompt.ShouldBeNull();
        result.Plan.Repository.ShouldBe(new RepositoryChoice("Northwind.Orders", new FolderPath(Directory), IsRepository: true));
    }

    [Fact]
    public void TakeANameAnOpeningPromptAndADescriptionFromTheArguments()
    {
        LaunchPlanResult result = HereCommand.Plan(Directory, ["fix-tests", "-p", "Fix the failing tests", "-d", "The flaky suite"], isRepository: _ => true);

        result.Plan.ShouldNotBeNull();
        result.Plan.Name.Text.ShouldBe("fix-tests");
        result.Plan.OpeningPrompt.ShouldBe("Fix the failing tests");
        result.Plan.Description.ShouldBe("The flaky suite");
    }

    [Fact]
    public void WorkInAFolderThatIsNotARepository()
    {
        LaunchPlanResult result = HereCommand.Plan(@"C:\notes", [], isRepository: _ => false);

        result.Plan.ShouldNotBeNull();
        result.Plan.Repository.IsRepository.ShouldBeFalse();
        result.Plan.Name.Text.ShouldBe("notes");
    }

    [Fact]
    public void RefuseBadArgumentsAndABadNameAsFacts()
    {
        HereCommand.Plan(Directory, ["--bogus"], isRepository: _ => true).Problem.ShouldBe(new LaunchProblem.UnknownOption("--bogus"));
        HereCommand.Plan(Directory, ["-p"], isRepository: _ => true).Problem.ShouldBe(new LaunchProblem.OptionNeedsValue("-p"));
        HereCommand.Plan(Directory, ["fix-123", "extra"], isRepository: _ => true).Problem.ShouldBe(new LaunchProblem.UnexpectedArgument("extra"));
        HereCommand.Plan(Directory, ["a;b"], isRepository: _ => true).Problem.ShouldBe(new LaunchProblem.InvalidName(NameProblem.UnsafeCharacters));
        HereCommand.Plan(Directory, [""], isRepository: _ => true).Problem.ShouldBe(new LaunchProblem.InvalidName(NameProblem.Missing));
    }
}