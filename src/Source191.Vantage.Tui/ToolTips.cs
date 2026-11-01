using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>Vantage's tooltips: black on amber, so they read as tooltips and not as more screen text.</summary>
internal static class ToolTips
{
    /// <summary>Each line keeps its breaks and a cell of margin either side, and the view is exactly as tall as its lines.</summary>
    public static Label View(string text)
    {
        string[] lines = [.. text.Split('\n').Select(line => $" {line} ")];
        Label label = new() { Width = lines.Max(line => line.Length), Height = lines.Length, Text = string.Join('\n', lines) };
        label.SetScheme(Palette.Pair(Palette.Background, Palette.Amber));

        return label;
    }
}