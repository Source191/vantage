using Source191.Vantage.Application.Views;
using Terminal.Gui.App;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// Stopped sessions look the same whether a reboot killed them or the user stopped them on purpose, so they are
/// offered as a checklist with nothing ticked. Failed sessions never come here; they resume automatically.
/// </summary>
internal static class ResumeChecklistDialog
{
    public static IReadOnlyList<SessionRow> Show(IApplication app, IReadOnlyList<SessionRow> stopped)
    {
        List<SessionRow> chosen = [];
        using Dialog dialog = new() { Title = "Resume stopped sessions", Width = 70, Height = Math.Min(8 + stopped.Count, 24) };
        dialog.SetScheme(Palette.Focusable(Palette.White));

        Label caption = new() { X = 1, Y = 0, Text = "Tick the sessions to resume. Deliberately stopped ones are best left alone." };
        caption.SetScheme(Palette.Plain(Palette.Orange));
        dialog.Add(caption);

        List<CheckBox> boxes = [];
        for (int index = 0; index < stopped.Count; index++)
        {
            SessionRow row = stopped[index];
            CheckBox box = new() { X = 1, Y = 2 + index, Text = $"{row.Session.Name}  ({row.Repository}, {row.Branch?.Value ?? "-"})", Value = CheckState.UnChecked };
            boxes.Add(box);
            dialog.Add(box);
        }

        Button resume = new() { Text = "_Resume", IsDefault = true };
        Button cancel = new() { Text = "_Cancel" };
        resume.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            chosen = [.. boxes.Select((box, index) => (box, index)).Where(pair => pair.box.Value == CheckState.Checked).Select(pair => stopped[pair.index])];
            app.RequestStop(dialog);
        };
        cancel.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.AddButton(resume);
        dialog.AddButton(cancel);

        app.Run(dialog);

        return chosen;
    }
}