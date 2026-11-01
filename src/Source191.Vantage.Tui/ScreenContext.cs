using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Tui;

/// <summary>
/// What the console screen is handed from the tool's <c>ConsoleComposition</c>: the tab title, the settings row for a width (asked for on
/// every redraw so a saved setting shows at once), whether a newer release is known, the clipboard the copy key writes to
/// the diagnostic log that loads, background failures and dialogs are recorded in, and the heartbeat that watches the UI
/// thread.
/// </summary>
public sealed record ScreenContext(string Title, Func<int, IReadOnlyList<IReadOnlyList<TextPart>>> SettingsRows, Func<bool> UpdateKnown, ScreenClipboard Clipboard, DiagnosticLog Diagnostics, Heartbeat Heartbeat);