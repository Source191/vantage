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

/// <summary>A spinner and a few words, centred over the empty table until the first rows arrive.</summary>
internal sealed class WaitingIndicator : View
{
    private readonly SpinnerView spinner;

    public WaitingIndicator(string text)
    {
        X = Pos.Center();
        Y = Pos.Center();
        Width = text.Length + 2;
        Height = 1;
        Visible = false;
        spinner = Spinners.Create(0, 0);
        Label label = new() { X = 2, Y = 0, Width = Dim.Fill(), Height = 1, Text = text };
        label.SetScheme(Palette.Plain(Palette.Cyan));
        Add(spinner, label);
    }

    public void Show(bool on)
    {
        Visible = on;
        spinner.Show(on);
    }
}