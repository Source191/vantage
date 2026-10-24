using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>
/// A question answered by typing a specific word, for actions that destroy work. The word is shown highlighted on
/// its own line and F2 copies it to the clipboard, so it can be pasted rather than typed. Enter in the box is the
/// only way to confirm, and the dialog stays open, saying what was typed, until the text matches or Esc cancels.
/// Returns the matching text, or null when cancelled.
/// </summary>
internal static class TypedConfirmationDialog
{
    public static string? Show(IApplication app, string title, string message, string expected, string expectedKind)
    {
        string? confirmed = null;
        using Dialog dialog = new() { Title = title, Width = 76, Height = 15 };
        dialog.SetScheme(Palette.Focusable(Palette.White));

        Label text = new() { X = 1, Y = 1, Width = Dim.Fill(1), Height = 2, Text = message };
        text.SetScheme(Palette.Plain(Palette.Amber));
        // The name sits in a read-only box, opened focused with the whole name selected, so Ctrl+C copies it
        // (or a mouse drag inside the box selects part of it); F2 copies it from anywhere in the dialog.
        TextField target = new() { X = 1, Y = 3, Width = Dim.Fill(1), Text = expected, ReadOnly = true };
        // A read-only field is drawn with the ReadOnly role once it loses focus, so every role gets the same colours.
        Attribute highlight = Palette.Highlight().Normal;
        target.SetScheme(new Scheme { Normal = highlight, Focus = highlight, HotNormal = highlight, HotFocus = highlight, Editable = highlight, ReadOnly = highlight, Disabled = highlight });
        Label caption = new() { X = 1, Y = 4, Text = "Ctrl+C copies it (or F2). Below: paste or type it, then Enter:" };
        TextField input = new() { X = 1, Y = 5, Width = Dim.Fill(1) };
        // The box must be visible before anything is typed, so it gets its own grey background (cyan once focused).
        Attribute box = new(Palette.White, Palette.Grey);
        input.SetScheme(new Scheme { Normal = box, Focus = Palette.Highlight().Normal, HotNormal = box, HotFocus = Palette.Highlight().Normal, Editable = box, Disabled = box });
        Label problem = new() { X = 1, Y = 6, Width = Dim.Fill(1), Height = 1, Text = " " };
        problem.SetScheme(Palette.Plain(Palette.Red));
        Label hint = new() { X = 1, Y = 7, Width = Dim.Fill(1), Height = 1, Text = "Enter removes once the text matches exactly. Esc keeps the worktree." };
        hint.SetScheme(Palette.Plain(Palette.Grey));
        dialog.Add(text, target, caption, input, problem, hint);
        target.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            input.SetFocus();
        };

        // No Remove button and no default button: a button would invite pressing it before anything was typed.
        // Enter in the box raises the field's Accepting event (verified with the harness), which is the only way in.
        input.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            string typed = input.Text;
            if (string.Equals(typed, expected, StringComparison.Ordinal))
            {
                confirmed = typed;
                app.RequestStop(dialog);
                return;
            }

            problem.Text = typed.Length == 0
                ? $"Nothing typed yet; the {expectedKind} is {expected}."
                : $"Not the same: you typed {typed}.";
        };
        dialog.KeyDown += (_, key) =>
        {
            if (key == GuiKey.F2)
            {
                key.Handled = true;
                hint.Text = app.Clipboard?.TrySetClipboardData(expected) == true
                    ? $"Copied {expected} to the clipboard; Ctrl+V pastes it into the box."
                    : "The clipboard is not available here; Shift+drag selects text in Windows Terminal.";
            }
        };
        Button copy = new() { Text = "Cop_y name (F2)" };
        copy.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            dialog.NewKeyDownEvent(GuiKey.F2);
            input.SetFocus();
        };
        Button cancel = new() { Text = "_Cancel (Esc)" };
        cancel.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.AddButton(copy);
        dialog.AddButton(cancel);
        dialog.Initialized += (_, _) =>
        {
            target.SetFocus();
            target.SelectAll();
        };

        app.Run(dialog);

        return confirmed;
    }
}