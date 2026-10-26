using Source191.Vantage.Application;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// A question with buttons, in Vantage's colours rather than Terminal.Gui's default blue. Returns the index of the
/// button pressed, or null when the dialog was left with Esc. The first button is the default.
/// </summary>
internal static class Confirm
{
    public static int? Query(IApplication app, DialogName name, string title, string message, params string[] buttons)
    {
        int? answer = null;
        string[] lines = message.Split('\n');
        int width = Math.Clamp(Math.Max(lines.Max(line => line.Length), title.Length) + 6, 40, 110);
        using Dialog dialog = new() { Title = title, Width = width, Height = lines.Length + 6 };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, dialog, name);

        Label text = new() { X = 1, Y = 1, Width = Dim.Fill(1), Height = lines.Length, Text = message };
        text.SetScheme(Palette.Plain(Palette.Amber));
        dialog.Add(text);

        for (int index = 0; index < buttons.Length; index++)
        {
            int chosen = index;
            Button button = new() { Text = $"_{buttons[index]}", IsDefault = index == 0 };
            button.Accepting += (_, accepting) =>
            {
                accepting.Handled = true;
                answer = chosen;
                app.RequestStop(dialog);
            };
            dialog.AddButton(button);
        }

        app.Run(dialog);

        return answer;
    }
}