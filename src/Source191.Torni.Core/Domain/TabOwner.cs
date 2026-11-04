namespace Source191.Torni.Domain;

/// <summary>Whose tab a registration is: a session's, or Torni's own.</summary>
public abstract record TabOwner
{
    public static readonly TabOwner Torni = new ForTorni();

    public sealed record ForSession(SessionId Id) : TabOwner;

    /// <summary>The tab Torni itself runs in, so its colour is not handed to a session tab.</summary>
    public sealed record ForTorni : TabOwner;
}