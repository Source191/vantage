using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Composition;
using System.CommandLine;

// Composition root, with the Composition folder: the process-wide start is here, each command's wiring in its own class.
// Everything is wiring; behaviour lives in the tested classes it assembles.
SharedParts parts = SharedParts.ForCurrentUser();
Terminal.Gui.App.Logging.Logger = new TerminalGuiBridge(parts.Log);
CrashRecorder crashes = parts.Crashes;
AppDomain.CurrentDomain.UnhandledException += (_, unhandled) =>
{
    if (unhandled.ExceptionObject is Exception exception)
    {
        crashes.Record(exception);
    }
};
TaskScheduler.UnobservedTaskException += (_, unobserved) => crashes.RecordUnobserved(unobserved.Exception);

// Help, version and parse errors are System.CommandLine's and need neither Claude Code nor a terminal tab.
RootCommand root = CommandLine.Build(new CommandActions(
    cancellationToken => crashes.GuardAsync(() => LaunchParts.RunAsync(parts, Console.Error, launch => ConsoleComposition.RunAsync(parts, launch, cancellationToken))),
    (name, prompt, cancellationToken) => crashes.GuardAsync(() => LaunchParts.RunAsync(parts, Console.Error, launch => HereComposition.RunAsync(parts, launch, name, prompt, cancellationToken))),
    (request, cancellationToken) => TerminalSetupComposition.RunAsync(parts, request, cancellationToken),
    // Run by the shell inside a tab Vantage opened, before claude attach starts; the shell passes its own $PID.
    async (registrationId, processId, _) =>
    {
        await new TabClaimer(parts.Store, parts.ProcessRegistry).ClaimAsync(registrationId, processId);
        return 0;
    }));

return await CommandLine.InvokeAsync(root, args, Console.Out, Console.Error);