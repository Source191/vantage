using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Application.Effects;

/// <summary>
/// Every background effect's handler, registered in one place: the tool's <c>ConsoleComposition</c> builds the real dispatcher here and
/// <c>EffectDispatcherShould</c> checks that no effect a reducer can produce is missing.
/// </summary>
public static class Composition
{
    public static EffectDispatcher Handlers(EffectPorts ports, LaunchService launches, TabScripts scripts, CurrentSettings settings, VantageProcess vantage, DiagnosticLog log)
    {
        OpenTabHandler tabs = new(ports.Tabs, ports.Processes, ports.Terminal, ports.Clock, scripts);
        SessionTabs sessionTabs = new(ports.Tabs, ports.Processes, ports.Clock);

        return new EffectDispatcher(log, ports.Clock)
            .Register(tabs)
            .Register(new StopSessionHandler(ports.Claude, sessionTabs))
            .Register(new AdoptSessionHandler(ports.Claude, ports.Processes, ports.Transcripts))
            .Register(new RenameSessionHandler(ports.Claude, ports.Transcripts, ports.Tabs, sessionTabs, tabs))
            .Register(new CloseEmptyTerminalsHandler(ports.Processes, ports.Transcripts))
            .Register(new ResumeSessionsHandler(ports.Claude, ports.Transcripts, tabs))
            .Register(new DeleteBranchHandler(ports.Git))
            .Register(new PruneRepositoryHandler(ports.Git))
            .Register(new OpenShellHandler(ports.Terminal))
            .Register(new StartSessionHandler(launches, tabs, ports.Terminal, scripts))
            .Register(new CopyFolderPathHandler(ports.Clipboard))
            .Register(new OpenProblemReportHandler(ports.Desktop, ports.CrashFile))
            .Register(new CopyProblemReportHandler(ports.Clipboard))
            .Register(new OpenSolutionHandler(ports.Desktop))
            .Register(new OpenPullRequestHandler(ports.Desktop))
            .Register(new OpenReleasePageHandler(ports.Desktop))
            .Register(new OpenInEditorHandler(ports.Editors, ports.Desktop, ports.Folders, settings))
            .Register(new SaveSettingsHandler(settings))
            .Register(new ToggleUpdateCheckHandler(settings))
            .Register(new StartUpdateHandler(ports.Terminal, scripts, vantage.ProcessId, vantage.Home));
    }
}