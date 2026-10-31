using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// Asks which solution to open when a folder holds several: a list of each one's path relative to the folder, whole and
/// one per line, the arrow keys to choose, Enter to open, Esc or Cancel to leave. Answers the chosen file, or
/// <see cref="SolutionChoice.NoneChosen"/>.
/// </summary>
internal static class ChooseSolutionDialog
{
    public static SolutionChoice Show(IApplication app, DiagnosticLog log, FolderPath folder, IReadOnlyList<string> files)
    {
        SolutionChoice answer = new SolutionChoice.NoneChosen();
        string question = Wording.ChooseSolutionQuestion(folder.Name);
        string[] paths = [.. files.Select(file => Path.GetRelativePath(folder.Value, file))];
        int width = Math.Clamp(Math.Max(paths.Max(path => path.Length), question.Length) + 6, 40, app.Screen.Width - 2);
        // The question, a blank row, the list, and the dialog's own border and button rows.
        int height = Math.Min(paths.Length + 8, app.Screen.Height - 2);
        using Dialog dialog = new() { Title = Wording.ChooseSolutionTitle, Width = width, Height = height };
        dialog.SetScheme(Palette.Focusable(Palette.White));
        DialogTabTitle.Apply(app, log, dialog, DialogName.OpenSolution);

        Label text = new() { X = 1, Y = 0, Width = Dim.Fill(1), Height = 1, Text = question };
        text.SetScheme(Palette.Plain(Palette.Amber));
        ListView list = new() { X = 1, Y = 2, Width = Dim.Fill(1), Height = Dim.Fill() };
        list.SetSource(new ObservableCollection<string>(paths));
        list.SelectedItem = 0;
        list.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            answer = list.SelectedItem is int chosen && chosen >= 0 && chosen < files.Count ? new SolutionChoice.Chosen(files[chosen]) : new SolutionChoice.NoneChosen();
            app.RequestStop(dialog);
        };
        dialog.Add(text, list);

        Button cancel = new() { Text = "_Cancel" };
        cancel.Accepting += (_, accepting) =>
        {
            accepting.Handled = true;
            app.RequestStop(dialog);
        };
        dialog.AddButton(cancel);
        dialog.Initialized += (_, _) => list.SetFocus();

        app.Run(dialog);

        return answer;
    }
}