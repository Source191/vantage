using Microsoft.Extensions.Logging;
using Source191.Vantage.Actions;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Diagnostics;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Launch;
using Source191.Vantage.Screens;
using Source191.Vantage.Screens.Tui;

// Composition root. Everything here is wiring; behaviour lives in the tested classes it assembles.
string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantage");
StateStore store = new(stateFolder);

// VANTAGE_LOG=<file> turns on a diagnostic log of Vantage's hand-offs and Terminal.Gui's internals.
ILogger log = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
string? logPath = Environment.GetEnvironmentVariable("VANTAGE_LOG");
if (!string.IsNullOrWhiteSpace(logPath))
{
    log = new FileLogger(logPath, TimeProvider.System);
    Terminal.Gui.App.Logging.Logger = log;
    log.Started(Environment.ProcessId);
}
ProcessRegistry processRegistry = new();

if (args.Length == 3 && args[0] == "claim-tab" && Guid.TryParse(args[1], out Guid registrationId) && int.TryParse(args[2], out int shellProcessId))
{
    // Run by the shell inside a tab Vantage opened, before claude attach starts; the shell passes its own $PID.
    await new TabClaimer(store, processRegistry).ClaimAsync(registrationId, shellProcessId);
    return 0;
}

string? claudeExecutable = ClaudeExecutable.Locate();
if (claudeExecutable is null)
{
    await Console.Error.WriteLineAsync("Claude Code was not found. Install it or put claude on the PATH.");
    return 1;
}

string vantageExecutable = Environment.ProcessPath ?? "vantage";
ProcessRunner processes = new();
ClaudeCli claude = new(processes, claudeExecutable);
GitCli git = new(processes);
VantageConfig config = await store.ReadConfigAsync();
ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
SessionRowEnricher enricher = new(git, store, processRegistry, TimeProvider.System, transcripts);
StateTransitionNotifier notifier = new(Console.Out);
WindowsTerminal terminal = new(processes);
LaunchService launchService = new(claude, git, store, new WorktreeIncludeCopier(git), TimeProvider.System);
TabOpener tabs = new(store, processRegistry, terminal, TimeProvider.System, claudeExecutable, vantageExecutable);
OwnTab ownTab = new(store, processRegistry, TimeProvider.System);
LaunchWorkflow launches = new(new RepositoryCatalogue(config.RepositoryRoots), store, launchService, tabs, terminal, claudeExecutable);
SessionActions actions = new(claude, git, store, processRegistry, transcripts);
TerminalEffects terminalEffects = new(claude, Console.Out);

if (args.Length >= 1 && args[0] == "here")
{
    return await RunHereAsync(args[1..]);
}

string context = string.Join('\n', Environment.MachineName, claudeExecutable, string.Join("; ", config.RepositoryRoots));
SessionsScreen sessionsScreen = new(LoadAsync, notifier, launches, actions, tabs, config.DefaultBase, TimeProvider.System, context);
WorktreeActions worktreeActions = new(git, store, terminal);
WorktreeInventory inventory = new(git, config.RepositoryRoots);
WorktreesScreen worktreesScreen = new(ScanAsync, worktreeActions, launches, config.DefaultBase, TimeProvider.System, context);
SessionsViewState sessions = SessionsViewState.Empty;
WorktreesViewState worktrees = WorktreesViewState.Empty;
bool showingWorktrees = false;

// This tab is named and coloured like a session tab so it can be found among them; the colour counts as in use.
// Claude sets the title itself while attached, so the label is written again after every hand-off.
TabColour? ownColour = await ownTab.ClaimAsync(Environment.ProcessId);
string ownTabLabel = TabEscapes.Title("Vantage") + (ownColour is null ? string.Empty : TabEscapes.Background(ownColour));
ConsoleRecovery.WriteSequence(Console.Out, ownTabLabel);

// Two screens alternate in one loop. Each keeps its state across switches and hand-offs, so a selection survives a trip
// to the other view or into an attached session.
while (true)
{
    ViewEffect? effect;
    if (showingWorktrees)
    {
        WorktreesScreenExit exit = worktreesScreen.Run(worktrees);
        worktrees = exit.State;
        effect = exit.Effect;
    }
    else
    {
        ScreenExit exit = sessionsScreen.Run(sessions);
        sessions = exit.State;
        effect = exit.Effect;
    }

    switch (effect)
    {
        case null or Quit:
            ConsoleRecovery.WriteSequence(Console.Out, TabEscapes.ClearTitle + TabEscapes.ResetBackground);
            return 0;
        case SwitchToWorktrees:
            showingWorktrees = true;
            continue;
        case SwitchToSessions:
            showingWorktrees = false;
            continue;
        case JumpToSession jump:
            showingWorktrees = false;
            sessions = sessions with { SelectedSessionId = sessions.Rows.FirstOrDefault(row => row.Session.Name == jump.SessionName)?.Session.SessionId ?? sessions.SelectedSessionId };
            continue;
        default:
            log.HandingOver(effect);
            await terminalEffects.RunAsync(effect, CancellationToken.None);
            log.ChildReturned();
            ConsoleRecovery.AfterChildProcess(Console.Out);
            ConsoleRecovery.WriteSequence(Console.Out, ownTabLabel);
            continue;
    }
}

// vantage here [name] [-p prompt] [-d description]: a supervised session in this folder, attached in this tab.
async Task<int> RunHereAsync(string[] hereArguments)
{
    string directory = Directory.GetCurrentDirectory();
    LaunchPlanResult planned = HereCommand.Plan(directory, hereArguments, path => Directory.Exists(Path.Combine(path, ".git")) || File.Exists(Path.Combine(path, ".git")));
    if (planned.Plan is null)
    {
        await Console.Error.WriteLineAsync(planned.Problem);
        return 2;
    }

    LaunchOutcome outcome = await launchService.LaunchAsync(planned.Plan, CancellationToken.None);
    switch (outcome.Kind)
    {
        case LaunchOutcomeKind.Started when outcome.JobId is not null:
            Console.WriteLine($"Started {planned.Plan.Name.Text} ({outcome.JobId}) in {directory}.");
            await terminalEffects.RunAsync(new AttachToSession(outcome.JobId), CancellationToken.None);
            ConsoleRecovery.AfterChildProcess(Console.Out);
            return 0;
        case LaunchOutcomeKind.WorkspaceNotTrusted:
            await Console.Error.WriteLineAsync($"Could not start: {directory} is not trusted yet. Run claude here once and accept the prompt.");
            return 1;
        default:
            await Console.Error.WriteLineAsync($"Could not start: {outcome.Message}");
            return 1;
    }
}

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