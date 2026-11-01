using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Files;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Packages;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.PullRequests;
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
        // With VANTAGE_LOG set, the counts are also written every 10 minutes; disposed as this returns, before the crash
        // recorder's exit write.
        using PeriodicCounts periodicCounts = new(TimeProvider.System, parts.Log, parts.Counts, parts.LogSetting);
        FolderPath home = FolderPath.From(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        CurrentSettings settings = await CurrentSettings.LoadAsync(parts.Store, home);
        ClaudeTranscripts transcripts = ClaudeTranscripts.ForCurrentUser();
        Folders folders = new();

        // Started before the rest of start-up and awaited just before the first screen, so it overlaps it.
        using CancellationTokenSource versionTimeout = new(TimeSpan.FromSeconds(5));
        Task<ClaudeVersionReport> claudeVersion = launch.Claude.VersionAsync(versionTimeout.Token);
        using PullRequestCatalogue pullRequests = PullRequests(parts, launch.Git);
        ScreenClipboard clipboard = new();
        using NuGetPackageVersions packageVersions = new(parts.Version, new HttpClientHandler());
        string updateCheckOptOut = Environment.GetEnvironmentVariable(UpdateCheckOptOut.VariableName) ?? string.Empty;
        UpdateCheckSwitch updateSwitch = new(settings, updateCheckOptOut);
        UpdateCheck updates = new(parts.Version, updateSwitch.IsOn, parts.Store, packageVersions, TimeProvider.System);
        EffectDispatcher dispatcher = Dispatcher(parts, launch, settings, home, transcripts, folders, clipboard);
        ClaudeVersionReport claudeVersionReport = await claudeVersion;
        Started start = parts.RecordStart(claudeVersionReport);

        DialogEffects dialogs = Dialogs(parts, launch, settings, folders, updates, start);
        ScreenContext context = Context(parts, claudeVersionReport, updates, clipboard);
        FolderLabels folderLabels = new(() => [.. settings.Value.RepositoryRoots, settings.Value.WorktreeRoot], home);
        ConsoleLoads loads = Loads(parts, launch, settings, folders, transcripts, pullRequests);
        ConsoleScreen consoleScreen = Screen(parts, launch, loads, context, dialogs, dispatcher, folderLabels);
        OwnTab ownTab = new(parts.Store, parts.ProcessRegistry, TimeProvider.System);
        TerminalConsole console = new(Console.Out, Wording.TabTitle);
        ScreenLoop loop = new(consoleScreen, ownTab, console, updates, parts.Log);

        return await loop.RunAsync(Environment.ProcessId, cancellationToken);
    }

    private static PullRequestCatalogue PullRequests(SharedParts parts, GitCli git)
    {
        GitHubPullRequests gitHub = new(parts.Processes, parts.Programs.Locate("gh"));
        AzureDevOpsPullRequests azureDevOps = new(parts.Processes, AzureCliExecutable.Locate(PathFolders.ForCurrentUser()));

        return new PullRequestCatalogue(git, new PullRequestHosts(gitHub, azureDevOps), TimeProvider.System, parts.Counts.Refreshes);
    }

    private static EffectDispatcher Dispatcher(
        SharedParts parts,
        LaunchParts launch,
        CurrentSettings settings,
        FolderPath home,
        ClaudeTranscripts transcripts,
        Folders folders,
        ScreenClipboard clipboard)
    {
        WindowsTerminal terminal = new(parts.Processes, parts.TerminalFragment, parts.Programs.Locate("wt"), parts.Programs.Locate("pwsh"));
        Desktop desktop = new(parts.Processes, parts.Programs.Locate("pwsh"));
        EditorLocator editors = new(folders, parts.Processes, EditorFolders.ForCurrentUser());
        EffectPorts ports = new(
            launch.Claude, launch.Git, parts.ProcessRegistry, transcripts, terminal, parts.Store,
            TimeProvider.System, clipboard, desktop, editors, folders, parts.CrashFile);
        TabScripts tabScripts = new(launch.Programs.Claude, parts.VantageExecutable);
        VantageProcess vantage = new(Environment.ProcessId, home);

        return HandlerComposition.Handlers(ports, launch.Launches, tabScripts, settings, vantage, parts.Log);
    }

    private static DialogEffects Dialogs(
        SharedParts parts, LaunchParts launch, CurrentSettings settings, Folders folders, UpdateCheck updates, Started start)
    {
        ProblemReports reports = new(parts.Log, parts.Counts.Processes, parts.CrashFile, start, settings, TimeProvider.System);
        SessionTabs sessionTabs = new(parts.Store, parts.ProcessRegistry, TimeProvider.System);
        DeleteSessionHandler deleteSession = new(launch.Claude, launch.Git, parts.Store, sessionTabs, folders, settings);
        RemoveWorktreeHandler removeWorktree = new(launch.Git, parts.Store, folders, settings);
        RepositoryCatalogue repositories = new(folders, settings);
        SolutionFinder solutions = new(folders);
        EditorPresets presets = new(new EditorLocator(folders, parts.Processes, EditorFolders.ForCurrentUser()));

        return new DialogEffects(
            repositories, parts.Store, folders, settings, solutions, updates, deleteSession, removeWorktree, reports, parts.ProcessRegistry, presets);
    }

    private static ScreenContext Context(SharedParts parts, ClaudeVersionReport claudeVersionReport, UpdateCheck updates, ScreenClipboard clipboard)
    {
        ClaudeCodeVersion claudeCode = ClaudeCodeSupport.Check(claudeVersionReport);
        HeaderSettingsRow header = new(parts.Version, claudeCode, updates, parts.ProcessRegistry, Environment.ProcessId);

        Heartbeat heartbeat = new(TimeProvider.System, parts.Counts.Stalls, parts.Log);

        return new ScreenContext(Wording.TabTitle, header.Rows, () => updates.Status is UpdateStatus.Available, clipboard, parts.Log, heartbeat);
    }

    private static ConsoleLoads Loads(
        SharedParts parts,
        LaunchParts launch,
        CurrentSettings settings,
        Folders folders,
        ClaudeTranscripts transcripts,
        PullRequestCatalogue pullRequests)
    {
        SessionRowEnricher enricher = new(launch.Git, parts.Store, parts.ProcessRegistry, TimeProvider.System, transcripts, pullRequests);
        WorktreeInventory inventory = new(launch.Git, folders, settings, pullRequests);
        SetupFactsReader setup = new(parts.TerminalFragment, ClaudeSettings.ForCurrentUser(), parts.Store, parts.Store.ConfigFile, parts.LogSetting);

        // The Sessions tab's listing is held for the worktrees scan, which waits for the first and runs none of its own.
        LatestSessions latestSessions = new(launch.Claude);

        return new ConsoleLoads(latestSessions, enricher, inventory, setup, settings, parts.Counts.Refreshes);
    }

    private static ConsoleScreen Screen(
        SharedParts parts,
        LaunchParts launch,
        ConsoleLoads loads,
        ScreenContext context,
        DialogEffects dialogs,
        EffectDispatcher dispatcher,
        FolderLabels folderLabels)
    {
        StateTransitionNotifier notifier = new(Console.Out);
        SessionActivity activity = new(TimeProvider.System);
        DetailLoader details = new(launch.Git, parts.Store, activity);
        RunningLoads scans = new();
        Func<CancellationToken, Task<ViewInput>> scanWorktrees = scans.Track<ViewInput>(loads.ScanWorktreesAsync);

        return new ConsoleScreen(
            loads.LoadSessionsAsync,
            scanWorktrees,
            scans,
            loads.ReadConfigAsync,
            loads.ConfigRowsWhileReading,
            notifier,
            activity,
            details,
            dialogs,
            dispatcher,
            TimeProvider.System,
            context,
            launch.Programs.Claude,
            folderLabels);
    }
}