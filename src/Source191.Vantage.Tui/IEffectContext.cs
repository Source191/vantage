using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Effects;
using Source191.Vantage.Application.Views;
using Terminal.Gui.App;

namespace Source191.Vantage.Tui;

/// <summary>What a dialog flow may do on the UI thread of the screen that raised it.</summary>
internal interface IEffectContext
{
    IApplication App { get; }

    /// <summary>The diagnostic log, which every dialog records itself in by name when it opens.</summary>
    DiagnosticLog Diagnostics { get; }

    /// <summary>The screen's key help, for the help dialog.</summary>
    string HelpText { get; }

    void ShowMessage(StatusMessage message);

    /// <summary>Feeds a follow-up input (a dialog's answer) back through the reducer.</summary>
    void Reduce(ViewInput input);

    /// <summary>Runs the effect's handler off the UI thread with a busy spinner; its answer becomes the status message.</summary>
    void Dispatch(ViewEffect effect);

    /// <summary>As above, with a continuation on the UI thread that turns the handler's answer into the status message.</summary>
    void Dispatch(ViewEffect effect, Func<Outcome, StatusMessage> onUiThread);

    /// <summary>Runs work that is not a dispatched effect (its outcome needs a question) with a continuation on the UI thread.</summary>
    void RunInBackground<T>(StatusMessage working, Func<CancellationToken, Task<T>> action, Func<T, StatusMessage> onUiThread);
}