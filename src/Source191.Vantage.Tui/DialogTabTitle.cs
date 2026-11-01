using Source191.Vantage.Application;
using Source191.Vantage.Application.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// Keeps a dialog's own title off Vantage's tab. Terminal.Gui turns on <see cref="BorderSettings.TerminalTitle"/> for
/// every Runnable, so a dialog (Terminal.Gui's own folder picker included, which is a Runnable but not a <see cref="Dialog"/>) would write its title
/// (<see href="https://learn.microsoft.com/windows/console/console-virtual-terminal-sequences">OSC 0</see>) when it becomes modal; this turns that off and writes
/// <see cref="Wording.TabTitleDuring"/> instead. The main window keeps the setting, so it writes "Vantage" back when
/// the dialog closes. Becoming modal is also when the dialog is recorded in the diagnostic log, by name only.
/// </summary>
internal static class DialogTabTitle
{
    public static void Apply(IApplication app, DiagnosticLog log, Runnable dialog, DialogName name)
    {
        dialog.Border.Settings &= ~BorderSettings.TerminalTitle;
        dialog.IsModalChanged += (_, modal) =>
        {
            if (modal.Value)
            {
                log.Record(new DialogShown(name));
                app.Driver?.SetTerminalTitle(Wording.TabTitleDuring(name));
            }
        };
    }
}