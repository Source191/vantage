using Source191.Vantage.Application.Actions;
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
    IDesktop Desktop);

/// <summary>
/// Every background effect's handler, registered in one place: <c>Program.cs</c> builds the real dispatcher here and
/// <c>EffectDispatcherShould</c> checks that no effect a reducer can produce is missing.
/// </summary>
public static class Composition
{
    public static EffectDispatcher Handlers(EffectPorts ports, LaunchService launches, TabScripts scripts)
    {
        OpenTabHandler tabs = new(ports.Tabs, ports.Processes, ports.Terminal, ports.Clock, scripts);

        return new EffectDispatcher()
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
            .Register(new OpenSolutionHandler(ports.Desktop));
    }
}