using Source191.Vantage.Application;
using Terminal.Gui.App;

namespace Source191.Vantage.Tui;

/// <summary>
/// The clipboard through Terminal.Gui, the one path the typed-confirmation dialog already uses. The application exists
/// only while a table screen runs, so the screen attaches it on start and detaches it on exit; outside a run the
/// clipboard is simply not available and the copy key says it was busy.
/// </summary>
public sealed class ScreenClipboard : Ports.IClipboard
{
    private static readonly NoClipboard None = new();

    private readonly SharedSnapshot<Ports.IClipboard> current = new(None);

    public bool TrySetText(string text) => current.Value.TrySetText(text);

    internal void Attach(IApplication app) => current.Replace(new ApplicationClipboard(app));

    internal void Detach() => current.Replace(None);

    /// <summary>No application runs, so there is no clipboard to set.</summary>
    private sealed class NoClipboard : Ports.IClipboard
    {
        public bool TrySetText(string text) => false;
    }

    /// <summary>
    /// The running application's clipboard. Terminal.Gui answers null for it until its driver is initialised, so it
    /// is asked for on each copy rather than once on attaching.
    /// </summary>
    private sealed class ApplicationClipboard(IApplication app) : Ports.IClipboard
    {
        public bool TrySetText(string text) => app.Clipboard is { } clipboard && clipboard.TrySetClipboardData(text);
    }
}
