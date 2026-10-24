using Source191.Vantage.Domain;

namespace Source191.Vantage.Launch;

/// <summary>
/// <c>vantage here [name] [-p prompt] [-d description]</c>: a supervised session in the current folder, on its current
/// branch, attached in the same tab. Sessions started this way are Vantage-managed from the outset.
/// </summary>
public static class HereCommand
{
    public const string Usage = "Usage: vantage here [name] [-p \"opening prompt\"] [-d \"description\"]";

    public static LaunchPlanResult Plan(string directory, IReadOnlyList<string> arguments, Func<string, bool> isRepository)
    {
        RepositoryPath path = new(directory);
        string? name = null;
        string prompt = string.Empty;
        string description = string.Empty;

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
                case "-d" or "--description" when hasValue:
                    description = remaining.Dequeue();
                    break;
                case "-p" or "--prompt" or "-d" or "--description":
                    return new LaunchPlanResult(Plan: null, $"{argument} needs a value. {Usage}");
                case var option when option.StartsWith('-'):
                    return new LaunchPlanResult(Plan: null, $"Unknown option {option}. {Usage}");
                case var text when name is null:
                    name = text;
                    break;
                default:
                    return new LaunchPlanResult(Plan: null, $"Unexpected argument {argument}. {Usage}");
            }
        }

        RepositoryChoice repository = new(path.Name, directory, isRepository(directory));
        LaunchRequest request = new(repository, name ?? path.Name, description, LaunchMode.CurrentBranch, WorktreeBase.RemoteDefault, prompt);

        return LaunchPlanBuilder.Build(request, _ => false);
    }
}