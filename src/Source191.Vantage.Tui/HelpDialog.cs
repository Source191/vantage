using Source191.Vantage.Application;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// The help: the view's keys and columns in a read-only box that scrolls, so every line is reachable on a short
/// terminal. The dialog is as tall as the text needs, up to nine tenths of the screen; focus starts in the text, so
/// the arrow keys and PageUp and PageDown move through it, and Esc or Close leaves.
/// </summary>
internal static class HelpDialog
{
    public static void Show(IApplication app, string title, string text)
    {
        string[] lines = text.Split('\n');
        int width = Math.Clamp(lines.Max(line => line.Length) + 6, 40, app.Screen.Width - 2);
        int height = Math.Min(lines.Length + 6, app.Screen.Height * 9 / 10);
        using Dialog dialog = new() { Title = title, Width = width, Height = height };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, dialog, DialogName.Keys);

        // TextView is marked obsolete in favour of the separate tui-cs Editor package; a read-only scrolling box does not justify another dependency.
#pragma warning disable CS0618
        TextView body = new() { X = 1, Y = 0, Width = Dim.Fill(1), Height = Dim.Fill(), ReadOnly = true, WordWrap = false, Text = text };
#pragma warning restore CS0618
        body.SetScheme(Palette.Plain(Palette.Amber));
        dialog.Add(body);

        Button close = new() { Text = "_Close" };
        close.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.AddButton(close);
        dialog.Initialized += (_, _) => body.SetFocus();

        app.Run(dialog);
    }
}