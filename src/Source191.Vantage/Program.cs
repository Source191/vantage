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
using Source191.Vantage.Application.Detail;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Application.Reporting;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
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

// Every program Vantage starts is found on the PATH once and run by its full path, never by a bare name that Windows
// would look for in the folder Vantage was started from first.
ProgramLocator programs = ProgramLocator.ForCurrentUser();

// The diagnostic log is always kept in memory; VANTAGE_LOG=<file> also writes it to that file.
string? logPath = Environment.GetEnvironmentVariable("VANTAGE_LOG");
DiagnosticLog log = new(TimeProvider.System, string.IsNullOrWhiteSpace(logPath) ? [] : [new DiagnosticFile(logPath)]);
Terminal.Gui.App.Logging.Logger = new TerminalGuiBridge(log);
ProcessCounts processCounts = new();
RecordedProcessRunner processes = new(new ProcessRunner(), log, processCounts, TimeProvider.System);
CrashFile crashFile = new(stateFolder);
CrashRecorder crashes = new(log, processCounts, crashFile);
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
    cancellationToken => crashes.GuardAsync(() => OpenConsoleAsync(cancellationToken)),
    (name, prompt, cancellationToken) => crashes.GuardAsync(() => StartHereAsync(name, prompt, cancellationToken)),
    SetUpTerminalAsync,
    ClaimTabAsync));

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

async Task<int> StartHereAsync(HereName name, string prompt, CancellationToken cancellationToken)
{
    string? claudeExecutable = await LocateClaudeAsync();
    if (claudeExecutable is null)
    {
        return 1;
    }

    string? gitExecutable = await LocateGitAsync();
    if (gitExecutable is null)
    {
        return 1;
    }

    ClaudeCli claude = new(processes, claudeExecutable);
    GitCli git = new(processes, gitExecutable);
    LaunchService launchService = new(claude, git, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);
    // here does not ask Claude Code for its version, so as not to slow the attach down.
    RecordStart(null);
    TerminalConsole hereConsole = new(Console.Out, Wording.TabTitle);
    HereCommandRunner runner = new(new Folders(), launchService, new TerminalEffects(claude, hereConsole), hereConsole, Console.Out, Console.Error);

    return await runner.RunAsync(Directory.GetCurrentDirectory(), name is HereName.Typed typed ? typed.Text : null, prompt, cancellationToken);
}

async Task<int> OpenConsoleAsync(CancellationToken cancellationToken)
{
    string? claudeExecutable = await LocateClaudeAsync();
    if (claudeExecutable is null)
    {
        return 1;
    }

    string? gitExecutable = await LocateGitAsync();
    if (gitExecutable is null)
    {
        return 1;
    }

    ClaudeCli claude = new(processes, claudeExecutable);
    string tabTitle = Wording.TabTitle;
    GitCli git = new(processes, gitExecutable);
    FolderPath home = FolderPath.From(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    CurrentSettings settings = await CurrentSettings.LoadAsync(store, home);
    ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
    WindowsTerminal terminal = new(processes, terminalFragment, programs.Locate("wt"));
    Folders folders = new();
    TerminalConsole console = new(Console.Out, tabTitle);
    LaunchService launchService = new(claude, git, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);

    // Started before the rest of start-up and awaited just before the first screen, so it overlaps it.
    using CancellationTokenSource versionTimeout = new(TimeSpan.FromSeconds(5));
    Task<string?> claudeVersion = claude.VersionAsync(versionTimeout.Token);
    using PullRequestCatalogue pullRequests = new(git, new PullRequestHosts(new GitHubPullRequests(processes, programs.Locate("gh")), new AzureDevOpsPullRequests(processes, AzureCliExecutable.Locate(Environment.GetEnvironmentVariable("PATH")))), TimeProvider.System);
    SessionRowEnricher enricher = new(git, store, processRegistry, TimeProvider.System, transcripts, pullRequests);
    StateTransitionNotifier notifier = new(Console.Out);
    SessionActivity activity = new(TimeProvider.System);
    OwnTab ownTab = new(store, processRegistry, TimeProvider.System);
    ScreenClipboard clipboard = new();
    using NuGetPackageVersions packageVersions = new(version, new HttpClientHandler());
    bool optOutOfUpdateCheck = UpdateCheckOptOut.Applies(Environment.GetEnvironmentVariable(UpdateCheckOptOut.VariableName));
    UpdateCheck updates = new(version, () => settings.Value.CheckForUpdates && !optOutOfUpdateCheck, store, packageVersions, TimeProvider.System);
    EffectPorts ports = new(claude, git, processRegistry, transcripts, terminal, store, TimeProvider.System, clipboard, new Desktop(processes, programs.Locate("pwsh")), new EditorLocator(folders, processes, EditorFolders.ForCurrentUser()), folders, crashFile);
    EffectDispatcher dispatcher = Composition.Handlers(ports, launchService, new TabScripts(claudeExecutable, vantageExecutable), settings, new VantageProcess(Environment.ProcessId, home), log);
    WorktreeInventory inventory = new(git, folders, settings, pullRequests);
    string? claudeVersionText = await claudeVersion;
    Started start = RecordStart(claudeVersionText);
    ProblemReports reports = new(log, processCounts, crashFile, start, settings);
    DialogEffects dialogs = new(new RepositoryCatalogue(folders, settings), store, folders, settings, new SolutionFinder(folders), updates, new DeleteSessionHandler(claude, git, store, new SessionTabs(store, processRegistry, TimeProvider.System), folders), new RemoveWorktreeHandler(git, store, folders), reports, processRegistry);

    ClaudeCodeVersion claudeCode = ClaudeCodeSupport.Check(claudeVersionText);
    ScreenContext context = new(tabTitle, _ => [Wording.SettingsRow(version, claudeCode, updates.Status, processRegistry.UsageOf(Environment.ProcessId)?.PrivateMemory ?? MemorySize.Zero)], () => updates.Status is UpdateStatus.Available, clipboard, log);
    FolderLabels folderLabels = new(() => [.. settings.Value.RepositoryRoots, settings.Value.WorktreeRoot], home);
    SetupFactsReader setup = new(terminalFragment, ClaudeSettings.ForCurrentUser(), store, store.ConfigFile, logPath);
    // The Sessions tab's listing is held for the worktrees scan, which waits for the first and runs none of its own.
    LatestSessions latestSessions = new(claude);
    RunningLoads scans = new();
    ConsoleScreen consoleScreen = new(LoadAsync, scans.Track<ViewInput>(ScanAsync), scans, ReadConfigAsync, () => ConfigRows.For(settings.Value, SetupFacts.Reading), notifier, activity, new DetailLoader(git, store, activity), dialogs, dispatcher, TimeProvider.System, context, claudeExecutable, folderLabels);
    ScreenLoop loop = new(consoleScreen, ownTab, console, updates, log);

    return await loop.RunAsync(Environment.ProcessId, cancellationToken);

    async Task<ViewInput> ReadConfigAsync(CancellationToken readCancellation) => new ConfigRowsLoaded(ConfigRows.For(settings.Value, await setup.ReadAsync(readCancellation)));

    async Task<ViewInput> LoadAsync(CancellationToken loadCancellation)
    {
        SessionListing listing = await latestSessions.ListAsync(loadCancellation);
        IReadOnlyList<SessionRow> rows = await enricher.EnrichAsync(listing.Sessions, loadCancellation);

        return new RowsLoaded(rows) { AutomatedRuns = enricher.Measure(listing.AutomatedRuns) };
    }

    async Task<ViewInput> ScanAsync(CancellationToken scanCancellation)
    {
        IReadOnlyList<Session> sessions = await latestSessions.SessionsAsync(scanCancellation);

        return new WorktreeRowsLoaded(await inventory.ScanAsync(sessions, scanCancellation));
    }
}

// git's full path, or null after the sentence saying it was not found: the console and vantage here both need it.
async Task<string?> LocateGitAsync()
{
    if (programs.Locate("git") is ProgramLocation.Found git)
    {
        return git.Path;
    }

    await Console.Error.WriteLineAsync(Wording.GitNotFound);

    return null;
}

// An executable's path, or null after the sentence saying why Claude Code cannot be run: a batch file is never run.
async Task<string?> LocateClaudeAsync()
{
    switch (ClaudeExecutable.Locate())
    {
        case ClaudeLocation.Found found:
            return found.Path;
        case ClaudeLocation.BatchFileOnly batchFile:
            await Console.Error.WriteLineAsync(Wording.ClaudeIsABatchFile(batchFile.Path));
            return null;
        default:
            await Console.Error.WriteLineAsync(Wording.ClaudeNotFound);
            return null;
    }
}

// The start facts: versions and the terminal's size, never a path or a name.
Started RecordStart(string? claudeVersionText)
{
    Version? claudeCodeVersion = ClaudeVersion.TryParse(claudeVersionText, out Version? parsed) ? parsed : null;
    Started start = new(version, claudeCodeVersion, Environment.OSVersion.Version, Environment.Version, Console.WindowWidth, Console.WindowHeight);
    log.Record(start);

    return start;
}