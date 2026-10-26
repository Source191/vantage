using Source191.Vantage.Domain;
using System.CommandLine;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// <c>vantage here [name] [-p prompt]</c>: a supervised session in the current folder, on its current
/// branch, attached in the same tab. Sessions started this way are Vantage-managed from the outset.
/// </summary>
public static class HereCommand
{
    private static readonly Option<string[]> Prompt = CommandLineSyntax.Repeatable("-p", "--prompt");
    private static readonly Argument<string> Name = new("name") { Arity = ArgumentArity.ZeroOrOne };
    private static readonly Command Syntax = new("here") { Prompt, Name };

    public static LaunchPlanResult Plan(string directory, IReadOnlyList<string> arguments, Func<string, bool> isRepository)
    {
        FolderPath path = FolderPath.From(directory);
        ParseResult parsed = CommandLineSyntax.Parse(Syntax, arguments);
        LaunchProblem? problem = CommandLineSyntax.FirstProblem<LaunchProblem>(
            Syntax,
            parsed,
            arguments,
            option => new LaunchProblem.UnknownOption(option),
            option => new LaunchProblem.OptionNeedsValue(option),
            argument => new LaunchProblem.UnexpectedArgument(argument));
        if (problem is not null)
        {
            return new LaunchPlanResult.Refused(problem);
        }

        RepositoryChoice repository = new(path.Name, path, isRepository(directory));
        LaunchRequest request = new(repository, parsed.GetValue(Name) ?? path.Name, LaunchMode.CurrentBranch, WorktreeBase.RemoteDefault, CommandLineSyntax.Last(parsed, Prompt) ?? string.Empty);

        // On the current branch no worktree is placed, so the folder itself stands in for the worktree root.
        return LaunchPlanBuilder.Build(request, worktreeRoot: path, _ => false);
    }
}