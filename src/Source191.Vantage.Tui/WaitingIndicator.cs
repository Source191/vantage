using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Vantage.Tui;

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