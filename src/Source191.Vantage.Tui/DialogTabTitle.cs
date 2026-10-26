using Source191.Vantage.Application;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
namespace Source191.Vantage.Tui;

/// <summary>
/// Keeps a dialog's own title off Vantage's tab. Terminal.Gui turns on <see cref="BorderSettings.TerminalTitle"/> for
/// every Runnable, so a dialog would write its title (OSC 0) when it becomes modal; this turns that off and writes
/// <see cref="Wording.TabTitleDuring"/> instead. The main window keeps the setting, so it writes "Vantage" back when
/// the dialog closes.
/// </summary>
internal static class DialogTabTitle
{
    public static void Apply(IApplication app, Dialog dialog, DialogName name)
    {
        dialog.Border.Settings &= ~BorderSettings.TerminalTitle;
        dialog.IsModalChanged += (_, modal) =>
        {
            if (modal.Value)
            {
                app.Driver?.SetTerminalTitle(Wording.TabTitleDuring(name));
            }
        };
    }
}