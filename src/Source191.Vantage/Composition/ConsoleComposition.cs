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
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Tui;
using HandlerComposition = Source191.Vantage.Application.Effects.Composition;

namespace Source191.Vantage.Composition;

/// <summary>The console: every tab, dialog and key handler, built once Claude Code and git are found.</summary>
internal static class ConsoleComposition
{
    public static async Task<int> RunAsync(SharedParts parts, LaunchParts launch, CancellationToken cancellationToken)
    {
        JsonFileStore store = parts.Store;
        ProcessRegistry processRegistry = parts.ProcessRegistry;
        ToolVersion version = parts.Version;
        string vantageExecutable = parts.VantageExecutable;
        TerminalFragment terminalFragment = parts.TerminalFragment;
        ProgramLocator programs = parts.Programs;
        DiagnosticLogSetting logSetting = parts.LogSetting;
        DiagnosticLog log = parts.Log;
        ProcessCounts processCounts = parts.ProcessCounts;
        RecordedProcessRunner processes = parts.Processes;
        CrashFile crashFile = parts.CrashFile;

        string claudeExecutable = launch.Programs.Claude;
        ClaudeCli claude = launch.Claude;
        string tabTitle = Wording.TabTitle;
        GitCli git = launch.Git;
        FolderPath home = FolderPath.From(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        CurrentSettings settings = await CurrentSettings.LoadAsync(store, home);
        ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
        WindowsTerminal terminal = new(processes, terminalFragment, programs.Locate("wt"), programs.Locate("pwsh"));
        Folders folders = new();
        TerminalConsole console = new(Console.Out, tabTitle);
        LaunchService launchService = launch.Launches;

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
        EffectDispatcher dispatcher = HandlerComposition.Handlers(ports, launchService, new TabScripts(claudeExecutable, vantageExecutable), settings, new VantageProcess(Environment.ProcessId, home), log);
        WorktreeInventory inventory = new(git, folders, settings, pullRequests);
        ClaudeVersionReport claudeVersionReport = await claudeVersion;
        Started start = parts.RecordStart(claudeVersionReport);
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
}