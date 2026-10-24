using Source191.Vantage.Application.Views;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>Translates Terminal.Gui keys into the reducer's library-free keys.</summary>
internal static class KeyMapping
{
    public static Key? From(GuiKey key)
    {
        if (key == GuiKey.Enter)
        {
            return Key.Enter;
        }

        if (key == GuiKey.Esc)
        {
            return Key.Escape;
        }

        if (key == GuiKey.CursorUp)
        {
            return Key.Up;
        }

        if (key == GuiKey.CursorDown)
        {
            return Key.Down;
        }

        if (key == GuiKey.Backspace)
        {
            return Key.Backspace;
        }

        string text = key.AsRune.ToString();

        return text.Length == 1 && !char.IsControl(text[0]) ? Key.Of(text[0]) : null;
    }
}