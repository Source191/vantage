namespace Source191.Vantage.Tui;

/// <summary>What both table screens are handed from <c>Program.cs</c>: the tab title and the header's first three values.</summary>
public sealed record ScreenContext(string Title, string HeaderValues);