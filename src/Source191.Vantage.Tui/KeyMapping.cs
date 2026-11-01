using Source191.Vantage.Application.Views;
using System.Diagnostics.CodeAnalysis;
using GuiKey = Terminal.Gui.Input.Key;

namespace Source191.Vantage.Tui;

/// <summary>Translates Terminal.Gui keys into the reducer's library-free keys.</summary>
internal static class KeyMapping
{
    /// <summary>The named keys the reducers know, each with its Terminal.Gui key.</summary>
    private static readonly (GuiKey Gui, Key Key)[] Named =
    [
        (GuiKey.Enter, Key.Enter),
        (GuiKey.Esc, Key.Escape),
        (GuiKey.CursorUp, Key.Up),
        (GuiKey.CursorDown, Key.Down),
        (GuiKey.CursorLeft, Key.Left),
        (GuiKey.CursorRight, Key.Right),
        (GuiKey.Backspace, Key.Backspace),
    ];

    /// <summary>Whether the reducers know this key (a named key, or a printable character), and the key they know it as.</summary>
    public static bool TryFrom(GuiKey key, [MaybeNullWhen(false)] out Key mapped)
    {
        foreach ((GuiKey gui, Key named) in Named)
        {
            if (key == gui)
            {
                mapped = named;
                return true;
            }
        }

        string text = key.AsRune.ToString();
        mapped = text.Length == 1 && !char.IsControl(text[0]) ? Key.Of(text[0]) : null;

        return mapped is not null;
    }
}