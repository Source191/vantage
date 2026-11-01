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