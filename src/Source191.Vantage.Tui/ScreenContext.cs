using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Tui;

/// <summary>What both table screens are handed from <c>Program.cs</c>: the tab title, the header's first four values (asked for on every redraw so a saved setting shows at once), the clipboard the copy key writes to and the diagnostic log that loads, background failures and dialogs are recorded in.</summary>
public sealed record ScreenContext(string Title, Func<string> HeaderValues, ScreenClipboard Clipboard, DiagnosticLog Diagnostics);