namespace Source191.Vantage.Ports;

/// <summary>A profile's bell sound: Windows Terminal's own, written by leaving <c>bellSound</c> out, or a .wav file.</summary>
public abstract record BellSound
{
    public sealed record TerminalDefault : BellSound;

    public sealed record File(string Path) : BellSound;
}