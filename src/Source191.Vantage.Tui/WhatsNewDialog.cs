using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using System.Text;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>
/// What changed, in a read-only box that scrolls (Tab reaches it, then the arrow keys and PageUp and PageDown), as tall as
/// the lines need up to nine tenths of the screen, like the help. Before an update its buttons are Update now, Release
/// page and Later, with Later the default so an Enter pressed as it opens does nothing; Release page leaves it open.
/// After an update it has Close alone. Esc closes it either way. Update now has no hotkey, so a second U never updates.
/// The lines are notes read from outside Vantage, already stripped of control characters, and the box shows them as
/// text: nothing in them is ever opened.
/// </summary>
internal static class WhatsNewDialog
{
    private static readonly Rune NoHotKey = new('￿');

    /// <summary>Before an update: true when Update now was chosen; Release page calls <paramref name="openReleasePage"/> and stays open.</summary>
    public static bool AskBeforeUpdate(IApplication app, DiagnosticLog log, string title, IReadOnlyList<string> lines, Action openReleasePage)
    {
        bool update = false;
        using Dialog dialog = Create(app, log, title, lines);

        Button updateNow = Plain(Wording.UpdateNowButton);
        updateNow.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            update = true;
            app.RequestStop(dialog);
        };
        Button releasePage = new() { Text = $"_{Wording.ReleasePageButton}" };
        releasePage.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            openReleasePage();
        };
        dialog.AddButton(updateNow);
        dialog.AddButton(releasePage);
        dialog.AddButton(Closing(app, dialog, $"_{Wording.LaterButton}"));

        app.Run(dialog);

        return update;
    }

    /// <summary>The first start of a new version: the lines and Close.</summary>
    public static void ShowAfterUpdate(IApplication app, DiagnosticLog log, string title, IReadOnlyList<string> lines)
    {
        using Dialog dialog = Create(app, log, title, lines);
        dialog.AddButton(Closing(app, dialog, $"_{Wording.CloseButton}"));

        app.Run(dialog);
    }

    private static Dialog Create(IApplication app, DiagnosticLog log, string title, IReadOnlyList<string> lines)
    {
        int width = Math.Clamp(Math.Max(lines.Max(line => line.Length), title.Length) + 6, 50, app.Screen.Width - 2);
        int height = Math.Min(lines.Count + 6, app.Screen.Height * 9 / 10);
        Dialog dialog = new() { Title = title, Width = width, Height = height };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.WhatsNew);

        // TextView is marked obsolete in favour of the separate tui-cs Editor package; a read-only scrolling box does not justify another dependency.
#pragma warning disable CS0618
        TextView body = new() { X = 1, Y = 0, Width = Dim.Fill(1), Height = Dim.Fill(), ReadOnly = true, WordWrap = false, Text = string.Join('\n', lines) };
#pragma warning restore CS0618
        body.SetScheme(Palette.Plain(Palette.Amber));
        dialog.Add(body);

        return dialog;
    }

    /// <summary>The default button, which takes the first focus and closes the dialog.</summary>
    private static Button Closing(IApplication app, Dialog dialog, string text)
    {
        Button button = new() { Text = text, IsDefault = true };
        button.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };

        return button;
    }

    private static Button Plain(string text) => new() { HotKeySpecifier = NoHotKey, Text = text };
}
