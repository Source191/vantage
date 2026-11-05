using Terminal.Gui.Views;

namespace Source191.Torni.Tui;

/// <summary>The two pieces every dialog repeats: a key's or button's action that marks its event handled, and a button that runs one.</summary>
internal static class DialogButtons
{
    /// <summary>Runs a key's or button's action and marks the event handled.</summary>
    public static bool Handled(Action action)
    {
        action();

        return true;
    }

    public static Button Button(string text, Action pressed)
    {
        Button button = new() { Text = text };
        button.Accepting += (_, accepting) => accepting.Handled = Handled(pressed);

        return button;
    }
}