using Source191.Torni.Application.Views;

namespace Source191.Torni.Tui;

/// <summary>Where a tab hands what is not its own: an effect that leaves the console, a tab switch, a question only the Sessions tab's rows answer, and a change the frame must redraw.</summary>
internal interface ITabHost
{
    void Leave(ViewEffect effect);

    void Switch(ISwitchesTab effect);

    RowDecision Consult(IConsultsSessions effect);

    void FrameChanged();
}