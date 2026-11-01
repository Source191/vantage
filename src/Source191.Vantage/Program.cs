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
using Source191.Vantage.Tui;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;

// Composition root. Everything here is wiring; behaviour lives in the tested classes it assembles.
string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantage");
JsonFileStore store = new(stateFolder, TimeProvider.System);
ProcessRegistry processRegistry = new();
ToolVersion version = ToolVersion.FromInformational(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty);
string vantageExecutable = Environment.ProcessPath ?? "vantage";
TerminalFragment terminalFragment = TerminalFragment.ForCurrentUser(Path.Combine(AppContext.BaseDirectory, "vantage.ico"));

// Every program Vantage starts is found on the PATH once and run by its full path, never by a bare name that Windows
// would look for in the folder Vantage was started from first
// (https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw).
ProgramLocator programs = ProgramLocator.ForCurrentUser();

// The diagnostic log is always kept in memory; VANTAGE_LOG=<file> also writes it to that file.
DiagnosticLogSetting logSetting = DiagnosticLogSetting.Read(Environment.GetEnvironmentVariable("VANTAGE_LOG"));
DiagnosticLog log = new(TimeProvider.System, logSetting is DiagnosticLogSetting.File logFile ? [new DiagnosticFile(logFile.Path)] : []);
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

Task<int> StartHereAsync(HereName name, string prompt, CancellationToken cancellationToken) =>
    WithRequiredProgramsAsync(found => StartHereWithAsync(found, name, prompt, cancellationToken));

async Task<int> StartHereWithAsync(RequiredPrograms.Found found, HereName name, string prompt, CancellationToken cancellationToken)
{
    ClaudeCli claude = new(processes, found.Claude);
    GitCli git = new(processes, found.Git);
    LaunchService launchService = new(claude, git, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);
    // here does not ask Claude Code for its version, so as not to slow the attach down.
    RecordStart(new ClaudeVersionReport.NotRead());
    TerminalConsole hereConsole = new(Console.Out, Wording.TabTitle);
    HereCommandRunner runner = new(new Folders(), launchService, new TerminalEffects(claude, hereConsole), hereConsole, Console.Out, Console.Error);

    return await runner.RunAsync(Directory.GetCurrentDirectory(), name, prompt, cancellationToken);
}

Task<int> OpenConsoleAsync(CancellationToken cancellationToken) => WithRequiredProgramsAsync(found => OpenConsoleWithAsync(found, cancellationToken));

async Task<int> OpenConsoleWithAsync(RequiredPrograms.Found found, CancellationToken cancellationToken)
{
    string claudeExecutable = found.Claude;
    ClaudeCli claude = new(processes, claudeExecutable);
    string tabTitle = Wording.TabTitle;
    GitCli git = new(processes, found.Git);
    FolderPath home = FolderPath.From(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    CurrentSettings settings = await CurrentSettings.LoadAsync(store, home);
    ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
    WindowsTerminal terminal = new(processes, terminalFragment, programs.Locate("wt"), programs.Locate("pwsh"));
    Folders folders = new();
    TerminalConsole console = new(Console.Out, tabTitle);
    LaunchService launchService = new(claude, git, store, store, new WorktreeIncludeCopier(git), TimeProvider.System);

    // Started before the rest of start-up and awaited just before the first screen, so it overlaps it.
    using CancellationTokenSource versionTimeout = new(TimeSpan.FromSeconds(5));
    Task<ClaudeVersionReport> claudeVersion = claude.VersionAsync(versionTimeout.Token);
    using PullRequestCatalogue pullRequests = new(git, new PullRequestHosts(new GitHubPullRequests(processes, programs.Locate("gh")), new AzureDevOpsPullRequests(processes, AzureCliExecutable.Locate(PathFolders.ForCurrentUser()))), TimeProvider.System);
    SessionRowEnricher enricher = new(git, store, processRegistry, TimeProvider.System, transcripts, pullRequests);
    StateTransitionNotifier notifier = new(Console.Out);
    SessionActivity activity = new(TimeProvider.System);
    OwnTab ownTab = new(store, processRegistry, TimeProvider.System);
    ScreenClipboard clipboard = new();
    using NuGetPackageVersions packageVersions = new(version, new HttpClientHandler());
    UpdateCheckSwitch updateSwitch = new(settings, Environment.GetEnvironmentVariable(UpdateCheckOptOut.VariableName) ?? string.Empty);
    UpdateCheck updates = new(version, updateSwitch.IsOn, store, packageVersions, TimeProvider.System);
    EffectPorts ports = new(claude, git, processRegistry, transcripts, terminal, store, TimeProvider.System, clipboard, new Desktop(processes, programs.Locate("pwsh")), new EditorLocator(folders, processes, EditorFolders.ForCurrentUser()), folders, crashFile);
    EffectDispatcher dispatcher = Composition.Handlers(ports, launchService, new TabScripts(claudeExecutable, vantageExecutable), settings, new VantageProcess(Environment.ProcessId, home), log);
    WorktreeInventory inventory = new(git, folders, settings, pullRequests);
    ClaudeVersionReport claudeVersionReport = await claudeVersion;
    Started start = RecordStart(claudeVersionReport);
    ProblemReports reports = new(log, processCounts, crashFile, start, settings);
    DialogEffects dialogs = new(new RepositoryCatalogue(folders, settings), store, folders, settings, new SolutionFinder(folders), updates, new DeleteSessionHandler(claude, git, store, new SessionTabs(store, processRegistry, TimeProvider.System), folders, settings), new RemoveWorktreeHandler(git, store, folders, settings), reports, processRegistry);

    ClaudeCodeVersion claudeCode = ClaudeCodeSupport.Check(claudeVersionReport);
    HeaderSettingsRow header = new(version, claudeCode, updates, processRegistry, Environment.ProcessId);
    ScreenContext context = new(tabTitle, header.Rows, () => updates.Status is UpdateStatus.Available, clipboard, log);
    FolderLabels folderLabels = new(() => [.. settings.Value.RepositoryRoots, settings.Value.WorktreeRoot], home);
    SetupFactsReader setup = new(terminalFragment, ClaudeSettings.ForCurrentUser(), store, store.ConfigFile, logSetting);
    // The Sessions tab's listing is held for the worktrees scan, which waits for the first and runs none of its own.
    LatestSessions latestSessions = new(claude);
    RunningLoads scans = new();
    ConsoleLoads loads = new(latestSessions, enricher, inventory, setup, settings);
    ConsoleScreen consoleScreen = new(loads.LoadSessionsAsync, scans.Track<ViewInput>(loads.ScanWorktreesAsync), scans, loads.ReadConfigAsync, loads.ConfigRowsWhileReading, notifier, activity, new DetailLoader(git, store, activity), dialogs, dispatcher, TimeProvider.System, context, claudeExecutable, folderLabels);
    ScreenLoop loop = new(consoleScreen, ownTab, console, updates, log);

    return await loop.RunAsync(Environment.ProcessId, cancellationToken);
}

// Claude Code and git, found once for the console and vantage here; the sentence and exit code 1 when either cannot be run.
async Task<int> WithRequiredProgramsAsync(Func<RequiredPrograms.Found, Task<int>> run)
{
    switch (RequiredPrograms.Check(ClaudeExecutable.Locate(), programs.Locate("git")))
    {
        case RequiredPrograms.Found found:
            return await run(found);
        case RequiredPrograms.Missing missing:
            await Console.Error.WriteLineAsync(missing.Sentence);
            return 1;
        default:
            throw new UnreachableException();
    }
}

// The start facts: versions and the terminal's size, never a path or a name.
Started RecordStart(ClaudeVersionReport claudeVersionReport)
{
    Started start = Started.From(version, claudeVersionReport, Environment.OSVersion.Version, Environment.Version, Console.WindowWidth, Console.WindowHeight);
    log.Record(start);

    return start;
}