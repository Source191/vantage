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
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Tui;
using System.CommandLine;
using System.Reflection;

// Composition root. Everything here is wiring; behaviour lives in the tested classes it assembles.
string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantage");
JsonFileStore store = new(stateFolder, TimeProvider.System);
ProcessRegistry processRegistry = new();
ToolVersion version = ToolVersion.FromInformational(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
string vantageExecutable = Environment.ProcessPath ?? "vantage";
TerminalFragment terminalFragment = TerminalFragment.ForCurrentUser(Path.Combine(AppContext.BaseDirectory, "vantage.ico"));

// The diagnostic log is always kept in memory; VANTAGE_LOG=<file> also writes it to that file.
DiagnosticLog log = new(TimeProvider.System, []);
string? logPath = Environment.GetEnvironmentVariable("VANTAGE_LOG");
if (!string.IsNullOrWhiteSpace(logPath))
{
    Terminal.Gui.App.Logging.Logger = new FileLogger(logPath, TimeProvider.System);
}

// Help, version and parse errors are System.CommandLine's and need neither Claude Code nor a terminal tab.
RootCommand root = CommandLine.Build(new CommandActions(OpenConsoleAsync, StartHereAsync, SetUpTerminalAsync, ClaimTabAsync));

return await CommandLine.InvokeAsync(root, args, Console.Out, Console.Error);

// Run by the shell inside a tab Vantage opened, before claude attach starts; the shell passes its own $PID.
async Task<int> ClaimTabAsync(Guid registrationId, int processId, CancellationToken cancellationToken)
{
    await new TabClaimer(store, processRegistry).ClaimAsync(registrationId, processId);

    return 0;
}

// Needs neither Claude Code nor a terminal tab, so Claude Code is not located.
async Task<int> SetUpTerminalAsync(TerminalSetupRequest request, CancellationToken cancellationToken)
{
    string windowsMedia = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
    TerminalSetupRunner setupRunner = new(new Folders(), terminalFragment, ClaudeSettings.ForCurrentUser(), vantageExecutable, windowsMedia, Console.In, Console.Out, Console.Error);

    return await setupRunner.RunAsync(request, cancellationToken);
}

async Task<int> StartHereAsync(string? name, string? prompt, CancellationToken cancellationToken)
{
    string? claudeExecutable = await LocateClaudeAsync();
    if (claudeExecutable is null)
    {
        return 1;
    }

    ProcessRunner processes = new();
    ClaudeCli claude = new(processes, claudeExecutable);
    GitCli git = new(processes);
    LaunchService launchService = new(claude, git, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);
    HereCommandRunner runner = new(new Folders(), launchService, new TerminalEffects(claude, Console.Out), new TerminalConsole(Console.Out, Wording.TabTitle), Console.Out, Console.Error);

    return await runner.RunAsync(Directory.GetCurrentDirectory(), name, prompt, cancellationToken);
}

async Task<int> OpenConsoleAsync(CancellationToken cancellationToken)
{
    string? claudeExecutable = await LocateClaudeAsync();
    if (claudeExecutable is null)
    {
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
    EffectDispatcher dispatcher = Composition.Handlers(ports, launchService, new TabScripts(claudeExecutable, vantageExecutable), settings, new VantageProcess(Environment.ProcessId, home), log);
    DialogEffects dialogs = new(new RepositoryCatalogue(folders, settings), store, folders, settings, new SolutionFinder(folders), updates, new DeleteSessionHandler(claude, git, store), new RemoveWorktreeHandler(git, store, folders));
    WorktreeInventory inventory = new(git, folders, settings, pullRequests);
    ClaudeCodeVersion claudeCode = ClaudeCodeSupport.Check(await claudeVersion);
    ScreenContext context = new(tabTitle, () => Wording.HeaderValues(version, claudeCode, settings.Value, updates.Status), clipboard);
    FolderLabels folderLabels = new(() => [.. settings.Value.RepositoryRoots, settings.Value.WorktreeRoot], home);
    SessionsScreen sessionsScreen = new(LoadAsync, notifier, dialogs, dispatcher, TimeProvider.System, context, claudeExecutable, folderLabels);
    WorktreesScreen worktreesScreen = new(ScanAsync, dialogs, dispatcher, TimeProvider.System, context, folderLabels);
    ScreenLoop loop = new(sessionsScreen, worktreesScreen, ownTab, console, terminalEffects, updates, log);

    return await loop.RunAsync(Environment.ProcessId, cancellationToken);

    async Task<ViewInput> LoadAsync(CancellationToken loadCancellation)
    {
        IReadOnlyList<Session> sessions = await claude.ListSessionsAsync(loadCancellation);
        IReadOnlyList<SessionRow> rows = await enricher.EnrichAsync(sessions, loadCancellation);

        return new RowsLoaded(rows);
    }

    async Task<ViewInput> ScanAsync(CancellationToken scanCancellation)
    {
        IReadOnlyList<Session> sessions = await claude.ListSessionsAsync(scanCancellation);

        return new WorktreeRowsLoaded(await inventory.ScanAsync(sessions, scanCancellation));
    }
}

async Task<string?> LocateClaudeAsync()
{
    string? claudeExecutable = ClaudeExecutable.Locate();
    if (claudeExecutable is null)
    {
        await Console.Error.WriteLineAsync("Claude Code was not found. Install it or put claude on the PATH.");
    }

    return claudeExecutable;
}