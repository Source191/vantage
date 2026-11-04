using Source191.Torni.Adapters.Claude;
using Source191.Torni.Adapters.Diagnostics;
using Source191.Torni.Adapters.Files;
using Source191.Torni.Adapters.Git;
using Source191.Torni.Adapters.Packages;
using Source191.Torni.Adapters.Processes;
using Source191.Torni.Adapters.PullRequests;
using Source191.Torni.Adapters.State;
using Source191.Torni.Adapters.Terminal;
using Source191.Torni.Application;
using Source191.Torni.Application.Actions;
using Source191.Torni.Application.Detail;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Effects;
using Source191.Torni.Application.Launch;
using Source191.Torni.Application.Reporting;
using Source191.Torni.Application.Views;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.Tui;
using HandlerComposition = Source191.Torni.Application.Effects.Composition;

namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Torni's console as <c>ConsoleComposition</c> builds it, with the scene's parts in place of the machine's: Claude
/// Code, gh and az answered from the scene, the state, transcripts and crash file in the scene folder, the scene's
/// processes in the process table, no update check and no log. Git, Windows Terminal and PowerShell are real.
/// Untested wiring, like the tool project's composition root; keep it in step with <c>ConsoleComposition</c>.
/// </summary>
internal sealed class SceneHost(SceneFolders folders, Scene scene, ToolVersion version, ScenePlay play)
{
    /// <summary>The name <c>tools\New-ReadmeScreenshot.ps1</c> gives the scene's window with <c>wt -w</c>.</summary>
    public const string WindowName = "readme-scene";

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        TimeProvider clock = TimeProvider.System;
        string ownPath = Environment.ProcessPath ?? throw new InvalidOperationException("The scene host cannot find its own path.");
        ProgramLocator programs = ProgramLocator.ForCurrentUser();
        string wt = programs.Locate("wt") is ProgramLocation.Found terminalFound ? terminalFound.Path : throw new InvalidOperationException("Windows Terminal (wt.exe) is not on the PATH.");
        SceneExecutables executables = new(ownPath, Path.Combine(folders.Bin, "gh.exe"), Path.Combine(folders.Bin, "az.cmd"), wt, WindowName);
        SceneLayout layout = new(folders.Home);
        SceneListing listing = new(scene, layout, clock);
        WriteTranscripts(listing);

        JsonFileStore store = new(folders.State, clock);
        SceneProcessRegistry processRegistry = new(listing, new ProcessRegistry());
        DiagnosticLog log = new(clock, []);
        ProcessCounts processCounts = new();
        PerformanceCounts counts = new(processCounts, new RefreshCounts(clock, log), new StallCounts());
        RecordedProcessRunner realProcesses = new(new ProcessRunner(TimeSpan.FromSeconds(60)), log, processCounts, clock);
        SceneProcessRunner processes = new(listing, new ScenePullRequests(scene, clock), executables, realProcesses);
        string git = programs.Locate("git") is ProgramLocation.Found found ? found.Path : throw new InvalidOperationException("git is not on the PATH.");
        TerminalFragment fragment = TerminalFragment.ForCurrentUser(Path.Combine(folders.Bin, "torni.ico"));
        CrashFile crashFile = new(folders.State);

        ClaudeCli claude = new(processes, executables.Claude);
        GitCli gitCli = new(processes, git);
        LaunchService launches = new(claude, gitCli, store, store, new WorktreeIncludeCopier(gitCli), clock);
        FolderPath home = FolderPath.From(folders.Home);
        CurrentSettings settings = await CurrentSettings.LoadAsync(store, home);
        ClaudeTranscripts transcripts = new(folders.ClaudeHome);
        Folders fileFolders = new();
        ClaudeVersionReport claudeVersion = await claude.VersionAsync(cancellationToken);
        Started start = Started.From(version, claudeVersion, Environment.OSVersion.Version, Environment.Version, Console.WindowWidth, Console.WindowHeight);

        using PullRequestCatalogue pullRequests = new(
            gitCli,
            new PullRequestHosts(new GitHubPullRequests(processes, new ProgramLocation.Found(executables.GitHub)), new AzureDevOpsPullRequests(processes, new ProgramLocation.Found(executables.AzureDevOps))),
            clock,
            counts.Refreshes);
        ScreenClipboard clipboard = new();
        using WingetPackageVersions packageVersions = new(version, new HttpClientHandler());
        UpdateCheck updates = new(version, () => false, store, packageVersions, clock);

        WindowsTerminal terminal = new(processes, fragment, programs.Locate("wt"), programs.Locate("pwsh"));
        Desktop desktop = new(processes, programs.Locate("pwsh"));
        EditorLocator editors = new(fileFolders, processes, EditorFolders.ForCurrentUser());
        EffectPorts ports = new(claude, gitCli, processRegistry, transcripts, terminal, store, clock, clipboard, desktop, editors, fileFolders, crashFile);
        TabScripts tabScripts = new(executables.Claude, ownPath);
        TorniProcess torni = new(Environment.ProcessId, home, new ProgramLocation.Missing());
        EffectDispatcher dispatcher = HandlerComposition.Handlers(ports, launches, tabScripts, settings, torni, log);

        ProblemReports reports = new(log, counts.Processes, crashFile, start, settings, clock);
        SessionTabs sessionTabs = new(store, processRegistry);
        WorktreeRemover remover = new(gitCli, store, fileFolders, settings);
        DeleteSessionHandler deleteSession = new(claude, gitCli, store, sessionTabs, fileFolders, remover);
        RemoveWorktreeHandler removeWorktree = new(gitCli, store, fileFolders, remover);
        DialogEffects dialogs = new(
            new RepositoryCatalogue(fileFolders, settings), store, fileFolders, settings, new SolutionFinder(fileFolders), updates, deleteSession, removeWorktree, reports, processRegistry,
            new EditorPresets(new EditorLocator(fileFolders, processes, EditorFolders.ForCurrentUser())));

        HeaderSettingsRow header = new(version, ClaudeCodeSupport.Check(claudeVersion), updates, processRegistry, Environment.ProcessId);
        ScreenContext context = new(Wording.TabTitle, header.Rows, () => false, updates.TakeAnnouncement, clipboard, log, new Heartbeat(clock, counts.Stalls, log));
        FolderLabels folderLabels = new(settings, home);
        SessionRowEnricher enricher = new(gitCli, store, processRegistry, clock, transcripts, pullRequests);
        WorktreeInventory inventory = new(gitCli, fileFolders, settings, pullRequests, log);
        SetupFactsReader setup = new(fragment, ClaudeSettings.ForCurrentUser(), store, store.ConfigFile, DiagnosticLogSetting.Read(null), log);
        ConsoleLoads loads = new(new LatestSessions(claude), enricher, inventory, setup, settings, counts.Refreshes);

        SessionActivity activity = new(clock);
        RunningLoads listings = new();
        RunningLoads scans = new();
        ConsoleScreen screen = new(
            listings.Track<ViewInput>(loads.LoadSessionsAsync),
            listings,
            scans.Track<ViewInput>(loads.ScanWorktreesAsync),
            scans,
            loads.ReadConfigAsync,
            loads.ConfigRowsWhileReading,
            new StateTransitionNotifier(Console.Out),
            activity,
            new DetailLoader(gitCli, store, activity, log),
            dialogs,
            dispatcher,
            clock,
            context,
            executables.Claude,
            folderLabels);
        OwnTab ownTab = new(store, processRegistry, clock);
        TerminalConsole console = new(Console.Out, Wording.TabTitle);
        WhatsNewOnStart whatsNew = new(version, store, new PackagedReleaseNotes(folders.Bin), log);
        ScreenLoop loop = new(screen, ownTab, console, updates, whatsNew, log);

        Task keysPlayed = PlayAsync(new KeyPlayer(folders, processes, executables), cancellationToken);
        Task closed = QuitWhenAskedAsync(cancellationToken);
        int exitCode = await loop.RunAsync(Environment.ProcessId, cancellationToken);
        await keysPlayed;
        await closed;

        return exitCode;
    }

    // Esc closes anything open, then q quits Torni; the tab's process exits 0 and Windows Terminal closes the tab.
    private async Task QuitWhenAskedAsync(CancellationToken cancellationToken)
    {
        while (!File.Exists(folders.CloseFile))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        ConsoleKeys.Press(NamedKey.Esc);
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        ConsoleKeys.Press(NamedKey.Esc);
        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
        ConsoleKeys.Type('q');
    }

    private async Task PlayAsync(KeyPlayer player, CancellationToken cancellationToken)
    {
        await player.PlayAsync(play.Setup, cancellationToken);
        if (play.FramesFolder.Length == 0 || !OperatingSystem.IsWindows())
        {
            await player.PlayAsync(play.Recorded, cancellationToken);
            return;
        }

        nint window = nint.Parse(await File.ReadAllTextAsync(folders.WindowFile, cancellationToken), System.Globalization.CultureInfo.InvariantCulture);
        Task recording = FrameRecorder.RecordAsync(window, play.FramesFolder, FrameSchedule.Animation.Offsets(KeyScript.Duration(play.Recorded)), cancellationToken);
        await player.PlayAsync(play.Recorded, cancellationToken);
        await recording;
    }

    // Enter opens a tab only for a session with a transcript; an empty file is enough for ClaudeTranscripts.
    private void WriteTranscripts(SceneListing listing)
    {
        string project = Path.Combine(folders.ClaudeHome, "projects", "readme-scene");
        Directory.CreateDirectory(project);
        foreach (string sessionId in listing.SessionIds())
        {
            File.WriteAllText(Path.Combine(project, $"{sessionId}.jsonl"), string.Empty);
        }
    }
}