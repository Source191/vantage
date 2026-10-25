namespace Source191.Vantage.Application;

/// <summary>What the command line asked for. Anything not recognised opens the console, as it always has.</summary>
public abstract record VantageCommand;

public sealed record OpenConsole : VantageCommand;

/// <summary><c>vantage here [name] [-p prompt]</c>; the arguments after <c>here</c> are parsed by <see cref="Launch.HereCommand"/>.</summary>
public sealed record StartHere(IReadOnlyList<string> Arguments) : VantageCommand;

/// <summary><c>vantage claim-tab &lt;registration id&gt; &lt;process id&gt;</c>, run by the shell inside a tab Vantage opened.</summary>
public sealed record ClaimTab(Guid RegistrationId, int ProcessId) : VantageCommand;

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
            _ => new OpenConsole(),
        };
    }
}