using Source191.Vantage.Application.Effects;

namespace Source191.Vantage.Application.Actions;

/// <summary>What opening a session's tab did: opened one, switched to the open one, or could not.</summary>
public abstract record OpenTabOutcome : Outcome
{
    /// <summary>Opened in the named colour; reused when every palette colour was already taken.</summary>
    public sealed record Opened(string Name, string Colour, bool ColourReused) : OpenTabOutcome;

    public sealed record SwitchedTo(string Name) : OpenTabOutcome;

    public sealed record NotFound(string Name) : OpenTabOutcome;
}