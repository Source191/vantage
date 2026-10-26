using System.CommandLine;

namespace Source191.Vantage.Application;

/// <summary>What the command line asked for. Only no arguments at all opens the console; anything not recognised is a usage error.</summary>
public abstract record VantageCommand;

public sealed record OpenConsole : VantageCommand;

/// <summary><c>vantage here [name] [-p prompt]</c>; the arguments after <c>here</c> are parsed by <see cref="Launch.HereCommand"/>.</summary>
public sealed record StartHere(IReadOnlyList<string> Arguments) : VantageCommand;

/// <summary><c>vantage claim-tab &lt;registration id&gt; &lt;process id&gt;</c>, run by the shell inside a tab Vantage opened.</summary>
public sealed record ClaimTab(Guid RegistrationId, int ProcessId) : VantageCommand;

/// <summary><c>vantage setup-terminal [--sound wav] [--session-sound wav] [--yes] [--remove]</c>; the arguments after it.</summary>
public sealed record SetupTerminal(IReadOnlyList<string> Arguments) : VantageCommand;

public sealed record Usage(string Message) : VantageCommand;

/// <summary><c>vantage --help</c>, or <c>--help</c> straight after a command: print that topic's help and exit 0.</summary>
public sealed record PrintHelp(HelpTopic Topic) : VantageCommand;

/// <summary><c>vantage --version</c> or <c>-v</c>: print the version and exit 0.</summary>
public sealed record PrintVersion : VantageCommand;

/// <summary>Whose help to print.</summary>
public enum HelpTopic
{
    Vantage,
    Here,
    SetupTerminal,
}

public static class CommandLine
{
    private static readonly Argument<Guid> RegistrationId = new("registration id");
    private static readonly Argument<int> ProcessId = new("process id");
    private static readonly Command ClaimTabSyntax = new("claim-tab") { RegistrationId, ProcessId };

    // here and setup-terminal parse their own arguments; the root only tells the commands apart.
    private static readonly Command HereSyntax = new("here") { TreatUnmatchedTokensAsErrors = false };
    private static readonly Command SetupTerminalSyntax = new("setup-terminal") { TreatUnmatchedTokensAsErrors = false };
    private static readonly Command Root = new("vantage") { HereSyntax, SetupTerminalSyntax, ClaimTabSyntax };

    public static VantageCommand Parse(string[] arguments)
    {
        return arguments switch
        {
            [] => new OpenConsole(),
            [string flag, ..] when IsHelp(flag) || flag is "?" or "help" => new PrintHelp(HelpTopic.Vantage),
            ["--version" or "-v", ..] => new PrintVersion(),
            ["here", string flag, ..] when IsHelp(flag) => new PrintHelp(HelpTopic.Here),
            ["setup-terminal", string flag, ..] when IsHelp(flag) => new PrintHelp(HelpTopic.SetupTerminal),
            [string first, .. string[] rest] => Choose(first, rest, CommandLineSyntax.Parse(Root, arguments)),
        };
    }

    private static VantageCommand Choose(string first, string[] rest, ParseResult parsed)
    {
        Command chosen = parsed.CommandResult.Command;
        if (chosen.Name != first)
        {
            return new Usage(Wording.UnknownCommand(first));
        }

        return chosen switch
        {
            _ when chosen == HereSyntax => new StartHere(rest),
            _ when chosen == SetupTerminalSyntax => new SetupTerminal(rest),
            _ when parsed.Errors.Count == 0 => new ClaimTab(parsed.GetValue(RegistrationId), parsed.GetValue(ProcessId)),
            _ => new Usage(Wording.ClaimTabUsage),
        };
    }

    /// <summary>The help spellings a command accepts after its name; <c>?</c> and <c>help</c> are help only on their own, since <c>vantage here help</c> names a session.</summary>
    private static bool IsHelp(string argument) => argument is "--help" or "-h" or "-?" or "/?";
}