namespace Source191.Vantage.Tui;

/// <summary>What both table screens are handed from <c>Program.cs</c>: the tab title, the header's first four values (asked for on every redraw so a saved setting shows at once) and the clipboard the copy key writes to.</summary>
public sealed record ScreenContext(string Title, Func<string> HeaderValues, ScreenClipboard Clipboard);