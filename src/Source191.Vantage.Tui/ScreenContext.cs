namespace Source191.Vantage.Tui;

/// <summary>What both table screens are handed from <c>Program.cs</c>: the tab title, the header's first three values and the clipboard the copy key writes to.</summary>
public sealed record ScreenContext(string Title, string HeaderValues, ScreenClipboard Clipboard);