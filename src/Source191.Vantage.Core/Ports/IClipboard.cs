namespace Source191.Vantage.Ports;

/// <summary>
/// The Windows clipboard, for plain text. Answers false when the clipboard is held by another program or unavailable,
/// so the caller can say so rather than fail. Implemented over Terminal.Gui's clipboard at the boundary.
/// </summary>
public interface IClipboard
{
    bool TrySetText(string text);
}