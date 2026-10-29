using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// The memory breakdown: the lines Wording laid out, one Label each, and a Close button (Esc closes too). One Label
/// per line with the hotkey mark off, because a program's name can hold an underscore (node_repl), which a Label
/// otherwise reads as its hotkey. The height leaves two rows above the button: with the usual six the last two lines
/// were never drawn (checked in the headless capture, Terminal.Gui 2.5.0).
/// </summary>
internal static class MemoryDialog
{
    private static readonly Rune NoHotKey = new('￿');

    public static void Show(IApplication app, DiagnosticLog log, string title, string text)
    {
        string[] lines = text.Split('\n');
        int width = Math.Clamp(Math.Max(lines.Max(line => line.Length), title.Length) + 6, 40, 110);
        using Dialog dialog = new() { Title = title, Width = width, Height = lines.Length + 8 };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.Memory);

        for (int index = 0; index < lines.Length; index++)
        {
            Label line = new() { HotKeySpecifier = NoHotKey, X = 1, Y = 1 + index, Width = Dim.Fill(1), Height = 1, Text = lines[index] };
            line.SetScheme(Palette.Plain(Palette.Amber));
            dialog.Add(line);
        }

        Button close = new() { Text = "_Close", IsDefault = true };
        close.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.AddButton(close);

        app.Run(dialog);
    }
}