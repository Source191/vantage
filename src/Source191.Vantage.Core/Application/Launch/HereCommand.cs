using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Launch;

/// <summary>
/// <c>vantage here [name] [-p prompt]</c>: a supervised session in the current folder, on its current
/// branch, attached in the same tab. Sessions started this way are Vantage-managed from the outset.
/// </summary>
public static class HereCommand
{
    public static LaunchPlanResult Plan(string directory, IReadOnlyList<string> arguments, Func<string, bool> isRepository)
    {
        FolderPath path = new(directory);
        string? name = null;
        string prompt = string.Empty;

        Queue<string> remaining = new(arguments);
        while (remaining.Count > 0)
        {
            string argument = remaining.Dequeue();
            bool hasValue = remaining.Count > 0;
            switch (argument)
            {
                case "-p" or "--prompt" when hasValue:
                    prompt = remaining.Dequeue();
                    break;
                case "-p" or "--prompt":
                    return new LaunchPlanResult.Refused(new LaunchProblem.OptionNeedsValue(argument));
                case var option when option.StartsWith('-'):
                    return new LaunchPlanResult.Refused(new LaunchProblem.UnknownOption(option));
                case var text when name is null:
                    name = text;
                    break;
                default:
                    return new LaunchPlanResult.Refused(new LaunchProblem.UnexpectedArgument(argument));
            }
        }

        RepositoryChoice repository = new(path.Name, path, isRepository(directory));
        LaunchRequest request = new(repository, name ?? path.Name, LaunchMode.CurrentBranch, WorktreeBase.RemoteDefault, prompt);

        return LaunchPlanBuilder.Build(request, _ => false);
    }
}