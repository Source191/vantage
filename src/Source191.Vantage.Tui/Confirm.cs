using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// A question with buttons, in Vantage's colours rather than Terminal.Gui's default blue. Answers true when the first
/// button, the default, was pressed; any other button or Esc answers false.
/// </summary>
internal static class Confirm
{
    public static bool Ask(IApplication app, DiagnosticLog log, DialogName name, string title, string message, params string[] buttons)
    {
        bool answer = false;
        string[] lines = message.Split('\n');
        int width = Math.Clamp(Math.Max(lines.Max(line => line.Length), title.Length) + 6, 40, 110);
        using Dialog dialog = new() { Title = title, Width = width, Height = lines.Length + 6 };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, name);

        Label text = new() { X = 1, Y = 1, Width = Dim.Fill(1), Height = lines.Length, Text = message };
        text.SetScheme(Palette.Plain(Palette.Amber));
        dialog.Add(text);

        for (int index = 0; index < buttons.Length; index++)
        {
            bool first = index == 0;
            Button button = new() { Text = $"_{buttons[index]}", IsDefault = first };
            button.Accepting += (_, accepting) =>
            {
                accepting.Handled = true;
                answer = first;
                app.RequestStop(dialog);
            };
            dialog.AddButton(button);
        }

        app.Run(dialog);

        return answer;
    }
}