namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>
/// What the command line said about one profile's bell sound: left off, so the Windows media default is used, or
/// given as typed (blank text included, which the runner refuses as a missing file).
/// </summary>
public abstract record SoundArgument
{
    public sealed record Omitted : SoundArgument;

    public sealed record Given(string Path) : SoundArgument;
}