using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

/// <summary>Terminal.Gui's own spinner, as the screens use it: braille dots, spinning only while shown.</summary>
internal static class Spinners
{
    public static SpinnerView Create(Pos x, Pos y)
    {
        SpinnerView spinner = new() { X = x, Y = y, Width = 1, Height = 1, Style = new SpinnerStyle.Dots(), SyncWithTerminal = false, Visible = false };
        spinner.SetScheme(Palette.Plain(Palette.Cyan));

        return spinner;
    }

    /// <summary>Shows and spins, or hides and stops; AutoSpin owns the timeout, so stopping it is what ends the animation.</summary>
    public static void Show(this SpinnerView spinner, bool on)
    {
        spinner.Visible = on;
        spinner.AutoSpin = on;
    }
}