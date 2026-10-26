using Microsoft.Extensions.Logging;
using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Diagnostics;
using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Packages;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.PullRequests;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Tui;
using System.Reflection;

// Composition root. Everything here is wiring; behaviour lives in the tested classes it assembles.
string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantage");
JsonFileStore store = new(stateFolder, TimeProvider.System);
ProcessRegistry processRegistry = new();
VantageCommand command = CommandLine.Parse(args);
ToolVersion version = ToolVersion.FromInformational(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
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
    case PrintHelp help:
        // Help and version need neither Claude Code nor a terminal tab.
        await Console.Out.WriteLineAsync(Wording.Help(help.Topic, version));
        return 0;
    case PrintVersion:
        await Console.Out.WriteLineAsync(Wording.VersionLine(version));
        return 0;
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
string tabTitle = Wording.TabTitle;
GitCli git = new(processes);
FolderPath home = FolderPath.From(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
CurrentSettings settings = await CurrentSettings.LoadAsync(store, home);
ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
WindowsTerminal terminal = new(processes, terminalFragment);
Folders folders = new();
TerminalConsole console = new(Console.Out, tabTitle);
LaunchService launchService = new(claude, git, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);
TerminalEffects terminalEffects = new(claude, Console.Out);

if (command is StartHere here)
{
    HereCommandRunner runner = new(folders, launchService, terminalEffects, console, Console.Out, Console.Error);

    return await runner.RunAsync(Directory.GetCurrentDirectory(), here.Arguments, CancellationToken.None);
}

// Started before the rest of start-up and awaited just before the first screen, so it overlaps it.
using CancellationTokenSource versionTimeout = new(TimeSpan.FromSeconds(5));
Task<string?> claudeVersion = claude.VersionAsync(versionTimeout.Token);
PullRequestCatalogue pullRequests = new(git, new PullRequestHosts(new GitHubPullRequests(processes), new AzureDevOpsPullRequests(processes, AzureCliExecutable.Locate(Environment.GetEnvironmentVariable("PATH")))), TimeProvider.System);
SessionRowEnricher enricher = new(git, store, processRegistry, TimeProvider.System, transcripts, pullRequests);
StateTransitionNotifier notifier = new(Console.Out);
OwnTab ownTab = new(store, processRegistry, TimeProvider.System);
ScreenClipboard clipboard = new();
using NuGetPackageVersions packageVersions = new(version);
bool optOutOfUpdateCheck = UpdateCheckOptOut.Applies(Environment.GetEnvironmentVariable(UpdateCheckOptOut.VariableName));
UpdateCheck updates = new(version, () => settings.Value.CheckForUpdates && !optOutOfUpdateCheck, store, packageVersions, TimeProvider.System);
EffectPorts ports = new(claude, git, processRegistry, transcripts, terminal, store, TimeProvider.System, clipboard, new Desktop(processes), new EditorLocator(folders, processes, EditorFolders.ForCurrentUser()), folders);
EffectDispatcher dispatcher = Composition.Handlers(ports, launchService, new TabScripts(claudeExecutable, vantageExecutable), settings, new VantageProcess(Environment.ProcessId, home));
DialogEffects dialogs = new(new RepositoryCatalogue(folders, settings), store, folders, settings, new SolutionFinder(folders), updates, new DeleteSessionHandler(claude, git, store), new RemoveWorktreeHandler(git, store, folders));
WorktreeInventory inventory = new(git, folders, settings, pullRequests);
string? claudeVersionText = await claudeVersion;
ScreenContext context = new(tabTitle, () => Wording.HeaderValues(version, claudeVersionText, settings.Value, updates.Status), clipboard);
FolderLabels folderLabels = new(() => [.. settings.Value.RepositoryRoots, settings.Value.WorktreeRoot], home);
SessionsScreen sessionsScreen = new(LoadAsync, notifier, dialogs, dispatcher, TimeProvider.System, context, claudeExecutable, folderLabels);
WorktreesScreen worktreesScreen = new(ScanAsync, dialogs, dispatcher, TimeProvider.System, context, folderLabels);
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