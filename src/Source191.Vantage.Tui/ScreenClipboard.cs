using Terminal.Gui.App;

namespace Source191.Vantage.Tui;

/// <summary>
/// The clipboard through Terminal.Gui, the one path the typed-confirmation dialog already uses. The application exists
/// only while a table screen runs, so the screen attaches it on start and detaches it on exit; outside a run the
/// clipboard is simply not available and the copy key says it was busy.
/// </summary>
public sealed class ScreenClipboard : Ports.IClipboard
{
    private IApplication? current;

    public bool TrySetText(string text) => Volatile.Read(ref current)?.Clipboard?.TrySetClipboardData(text) == true;

    internal void Attach(IApplication app) => Volatile.Write(ref current, app);

    internal void Detach() => Volatile.Write(ref current, null);
}