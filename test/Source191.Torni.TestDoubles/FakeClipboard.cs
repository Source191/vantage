using Source191.Torni.Ports;

namespace Source191.Torni.TestDoubles;

/// <summary>Holds the last text put on it, or refuses as if another program held the clipboard.</summary>
public sealed class FakeClipboard : IClipboard
{
    public string? Text { get; private set; }

    public bool Busy { get; set; }

    public bool TrySetText(string text)
    {
        if (Busy)
        {
            return false;
        }

        Text = text;

        return true;
    }
}