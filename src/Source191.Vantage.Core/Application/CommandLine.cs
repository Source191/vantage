namespace Source191.Vantage.Application;

/// <summary>What the command line asked for. Only no arguments at all opens the console; anything not recognised is a usage error.</summary>
public abstract record VantageCommand;

public sealed record OpenConsole : VantageCommand;

/// <summary><c>vantage here [name] [-p prompt]</c>; the arguments after <c>here</c> are parsed by <see cref="Launch.HereCommand"/>.</summary>
public sealed record StartHere(IReadOnlyList<string> Arguments) : VantageCommand;

/// <summary><c>vantage claim-tab &lt;registration id&gt; &lt;process id&gt;</c>, run by the shell inside a tab Vantage opened.</summary>
public sealed record ClaimTab(Guid RegistrationId, int ProcessId) : VantageCommand;

/// <summary><c>vantage setup-terminal [--sound wav] [--session-sound wav] [--yes] [--remove]</c>; the arguments after it are parsed by <see cref="TerminalSetup.TerminalSetupCommand"/>.</summary>
public sealed record SetupTerminal(IReadOnlyList<string> Arguments) : VantageCommand;

public sealed record Usage(string Message) : VantageCommand;

public static class CommandLine
{
    public static VantageCommand Parse(string[] arguments)
    {
        return arguments switch
        {
            [] => new OpenConsole(),
            ["here", .. string[] rest] => new StartHere(rest),
            ["claim-tab", string id, string pid] when Guid.TryParse(id, out Guid registrationId) && int.TryParse(pid, out int processId) => new ClaimTab(registrationId, processId),
            ["claim-tab", ..] => new Usage(Wording.ClaimTabUsage),
            ["setup-terminal", .. string[] rest] => new SetupTerminal(rest),
            [string unknown, ..] => new Usage(Wording.UnknownCommand(unknown)),
        };
    }
}