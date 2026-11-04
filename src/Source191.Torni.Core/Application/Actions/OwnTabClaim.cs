using Source191.Torni.Domain;

namespace Source191.Torni.Application.Actions;

/// <summary>Torni's own tab claimed in a colour, or left unregistered, when it is titled alone.</summary>
public abstract record OwnTabClaim
{
    public sealed record Claimed(TabColour Colour) : OwnTabClaim;

    public sealed record Unregistered : OwnTabClaim;
}