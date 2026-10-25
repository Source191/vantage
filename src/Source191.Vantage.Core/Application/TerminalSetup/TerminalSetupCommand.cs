namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>What <c>vantage setup-terminal</c> was asked to do; a null sound means "use the default".</summary>
public sealed record TerminalSetupRequest(string? Sound, string? SessionSound, bool Yes, bool Remove);

/// <summary>Why setup-terminal will not run, as a fact; <see cref="Wording"/> says it.</summary>
public abstract record TerminalSetupProblem
{
    public sealed record UnknownOption(string Option) : TerminalSetupProblem;

    public sealed record OptionNeedsValue(string Option) : TerminalSetupProblem;

    public sealed record UnexpectedArgument(string Argument) : TerminalSetupProblem;

    public sealed record RemoveWithSound : TerminalSetupProblem;

    public sealed record SoundNotFound(string Path) : TerminalSetupProblem;

    public sealed record SoundNotWav(string Path) : TerminalSetupProblem;
}

/// <summary>A request, or the reason there is none.</summary>
public sealed record TerminalSetupParse(TerminalSetupRequest? Request, TerminalSetupProblem? Problem)
{
    public static TerminalSetupParse Refused(TerminalSetupProblem problem) => new TerminalSetupParse(Request: null, problem);
}

/// <summary><c>vantage setup-terminal [--sound wav] [--session-sound wav] [--yes] [--remove]</c>: pure parsing, checking the files is the runner's.</summary>
public static class TerminalSetupCommand
{
    public static TerminalSetupParse Parse(IReadOnlyList<string> arguments)
    {
        string? sound = null;
        string? sessionSound = null;
        bool yes = false;
        bool remove = false;

        Queue<string> remaining = new(arguments);
        while (remaining.Count > 0)
        {
            string argument = remaining.Dequeue();
            bool hasValue = remaining.Count > 0;
            switch (argument)
            {
                case "--sound" when hasValue:
                    sound = remaining.Dequeue();
                    break;
                case "--session-sound" when hasValue:
                    sessionSound = remaining.Dequeue();
                    break;
                case "--sound" or "--session-sound":
                    return TerminalSetupParse.Refused(new TerminalSetupProblem.OptionNeedsValue(argument));
                case "--yes":
                    yes = true;
                    break;
                case "--remove":
                    remove = true;
                    break;
                case var option when option.StartsWith('-'):
                    return TerminalSetupParse.Refused(new TerminalSetupProblem.UnknownOption(option));
                default:
                    return TerminalSetupParse.Refused(new TerminalSetupProblem.UnexpectedArgument(argument));
            }
        }

        if (remove && (sound is not null || sessionSound is not null))
        {
            return TerminalSetupParse.Refused(new TerminalSetupProblem.RemoveWithSound());
        }

        return new TerminalSetupParse(new TerminalSetupRequest(sound, sessionSound, yes, remove), Problem: null);
    }
}