using Microsoft.Extensions.Logging;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Diagnostics;
using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.Tui;

// Composition root. Everything here is wiring; behaviour lives in the tested classes it assembles.
string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantage");
JsonFileStore store = new(stateFolder, TimeProvider.System);
ProcessRegistry processRegistry = new();
VantageCommand command = CommandLine.Parse(args);
string vantageExecutable = Environment.ProcessPath ?? "vantage";
TerminalFragment terminalFragment = TerminalFragment.ForCurrentUser();

// VANTAGE_LOG=<file> turns on a diagnostic log of Vantage's hand-offs and Terminal.Gui's internals.
ILogger log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
string? logPath = Environment.GetEnvironmentVariable("VANTAGE_LOG");
if (!string.IsNullOrWhiteSpace(logPath))
{
    log = new FileLogger(logPath, TimeProvider.System);
    Terminal.Gui.App.Logging.Logger = log;
    log.Started(Environment.ProcessId);
}

switch (command)
{
    case ClaimTab claim:
        // Run by the shell inside a tab Vantage opened, before claude attach starts; the shell passes its own $PID.
        await new TabClaimer(store, processRegistry).ClaimAsync(claim.RegistrationId, claim.ProcessId);
        return 0;
    case SetupTerminal setup:
        // Needs neither Claude Code nor a terminal tab, so it runs before Claude Code is located.
        string windowsMedia = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
        TerminalSetupRunner setupRunner = new(new Folders(), terminalFragment, ClaudeSettings.ForCurrentUser(), vantageExecutable, windowsMedia, Console.In, Console.Out, Console.Error);

        return await setupRunner.RunAsync(setup.Arguments, CancellationToken.None);
    case Usage usage:
        await Console.Error.WriteLineAsync(usage.Message);
        return 2;
    default:
        break;
}

string? claudeExecutable = ClaudeExecutable.Locate();
if (claudeExecutable is null)
{
    await Console.Error.WriteLineAsync("Claude Code was not found. Install it or put claude on the PATH.");
    return 1;
}

ProcessRunner processes = new();
ClaudeCli claude = new(processes, claudeExecutable);
GitCli git = new(processes);
VantageConfig config = await store.ReadConfigAsync();
ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
WindowsTerminal terminal = new(processes, terminalFragment);
Folders folders = new();
TerminalConsole console = new(Console.Out);
LaunchService launchService = new(claude, git, store, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);
TerminalEffects terminalEffects = new(claude, Console.Out);

if (command is StartHere here)
{
    HereCommandRunner runner = new(folders, launchService, terminalEffects, console, Console.Out, Console.Error);

    return await runner.RunAsync(Directory.GetCurrentDirectory(), here.Arguments, CancellationToken.None);
}

SessionRowEnricher enricher = new(git, store, store, processRegistry, TimeProvider.System, transcripts);
StateTransitionNotifier notifier = new(Console.Out);
OwnTab ownTab = new(store, processRegistry, TimeProvider.System);
EffectPorts ports = new(claude, git, processRegistry, transcripts, terminal, store, TimeProvider.System);
EffectDispatcher dispatcher = Composition.Handlers(ports, launchService, new TabScripts(claudeExecutable, vantageExecutable));
DialogEffects dialogs = new(new RepositoryCatalogue(folders, config.RepositoryRoots), store, folders, config.DefaultBase, new DeleteSessionHandler(claude, git, store), new RemoveWorktreeHandler(git, store, folders));
WorktreeInventory inventory = new(git, folders, config.RepositoryRoots);
string context = string.Join('\n', Environment.MachineName, claudeExecutable, string.Join("; ", config.RepositoryRoots));
SessionsScreen sessionsScreen = new(LoadAsync, notifier, dialogs, dispatcher, TimeProvider.System, context);
WorktreesScreen worktreesScreen = new(ScanAsync, dialogs, dispatcher, TimeProvider.System, context);
ScreenLoop loop = new(sessionsScreen, worktreesScreen, ownTab, console, terminalEffects, log);

return await loop.RunAsync(Environment.ProcessId, CancellationToken.None);

async Task<ViewInput> LoadAsync(CancellationToken cancellationToken)
{
    IReadOnlyList<Session> sessions = await claude.ListSessionsAsync(cancellationToken);
    IReadOnlyList<SessionRow> rows = await enricher.EnrichAsync(sessions, cancellationToken);

    return new RowsLoaded(rows);
}

async Task<ViewInput> ScanAsync(CancellationToken cancellationToken)
{
    IReadOnlyList<Session> sessions = await claude.ListSessionsAsync(cancellationToken);

    return new WorktreeRowsLoaded(await inventory.ScanAsync(sessions, cancellationToken));
}