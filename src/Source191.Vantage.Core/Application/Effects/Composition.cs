using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Effects;

/// <summary>The ports the background effect handlers are built from.</summary>
public sealed record EffectPorts(
    IClaudeCli Claude,
    IGit Git,
    IProcessRegistry Processes,
    ITranscripts Transcripts,
    IWindowsTerminal Terminal,
    ITabRegistry Tabs,
    TimeProvider Clock,
    IClipboard Clipboard,
    IDesktop Desktop,
    IEditorLocator Editors,
    IFolders Folders,
    ICrashFile CrashFile);

/// <summary>This Vantage's process, which the update tab waits for, and the folder that tab starts in.</summary>
public sealed record VantageProcess(int ProcessId, FolderPath Home);

/// <summary>
/// Every background effect's handler, registered in one place: <c>Program.cs</c> builds the real dispatcher here and
/// <c>EffectDispatcherShould</c> checks that no effect a reducer can produce is missing.
/// </summary>
public static class Composition
{
    public static EffectDispatcher Handlers(EffectPorts ports, LaunchService launches, TabScripts scripts, CurrentSettings settings, VantageProcess vantage, DiagnosticLog log)
    {
        OpenTabHandler tabs = new(ports.Tabs, ports.Processes, ports.Terminal, ports.Clock, scripts);

        return new EffectDispatcher(log, ports.Clock)
            .Register(tabs)
            .Register(new StopSessionHandler(ports.Claude))
            .Register(new AdoptSessionHandler(ports.Claude, ports.Processes, ports.Transcripts))
            .Register(new RenameSessionHandler(ports.Claude, ports.Transcripts))
            .Register(new CloseEmptyTerminalsHandler(ports.Processes, ports.Transcripts))
            .Register(new ResumeSessionsHandler(ports.Claude, ports.Transcripts))
            .Register(new DeleteBranchHandler(ports.Git))
            .Register(new PruneRepositoryHandler(ports.Git))
            .Register(new OpenShellHandler(ports.Terminal))
            .Register(new StartSessionHandler(launches, tabs, ports.Terminal, scripts))
            .Register(new CopyFolderPathHandler(ports.Clipboard))
            .Register(new OpenProblemReportHandler(ports.Desktop, ports.CrashFile))
            .Register(new CopyProblemReportHandler(ports.Clipboard))
            .Register(new OpenSolutionHandler(ports.Desktop))
            .Register(new OpenPullRequestHandler(ports.Desktop))
            .Register(new OpenInEditorHandler(ports.Editors, ports.Desktop, ports.Folders, settings))
            .Register(new SaveSettingsHandler(settings))
            .Register(new StartUpdateHandler(ports.Terminal, scripts, vantage.ProcessId, vantage.Home));
    }
}