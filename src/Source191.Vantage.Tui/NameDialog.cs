using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Source191.Vantage.Tui;

/// <summary>Asks for one line of text, pre-filled. Enter in the box answers what was typed; Esc or Cancel answers <see cref="TypedAnswer.Cancelled"/>.</summary>
internal static class NameDialog
{
    public static TypedAnswer Show(IApplication app, DiagnosticLog log, DialogName name, string title, string message, string current)
    {
        TypedAnswer answer = new TypedAnswer.Cancelled();
        using Dialog dialog = new() { Title = title, Width = 72, Height = 11 };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, name);

        Label text = new() { X = 1, Y = 1, Width = Dim.Fill(1), Height = 2, Text = message };
        text.SetScheme(Palette.Plain(Palette.Amber));
        Label caption = new() { X = 1, Y = 3, Text = "New name, then Enter:" };
        TextField input = new() { X = 1, Y = 4, Width = Dim.Fill(1), Text = current };
        Attribute box = new(Palette.White, Palette.FieldBackground);
        input.SetScheme(new Scheme { Normal = box, Focus = Palette.Highlight().Normal, HotNormal = box, HotFocus = Palette.Highlight().Normal, Editable = box, Disabled = box });
        Label hint = new() { X = 1, Y = 5, Width = Dim.Fill(1), Height = 1, Text = "Esc keeps the current name." };
        hint.SetScheme(Palette.Plain(Palette.Grey));
        dialog.Add(text, caption, input, hint);

        input.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            answer = new TypedAnswer.Typed(input.Text);
            app.RequestStop(dialog);
        };
        Button cancel = new() { Text = "_Cancel (Esc)" };
        cancel.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.AddButton(cancel);
        dialog.Initialized += (_, _) => input.SetFocus();

        app.Run(dialog);

        return answer;
    }
}