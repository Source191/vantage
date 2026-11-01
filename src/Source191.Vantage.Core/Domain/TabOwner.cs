namespace Source191.Vantage.Domain;

/// <summary>Whose tab a registration is: a session's, or Vantage's own.</summary>
public abstract record TabOwner
{
    public static readonly TabOwner Vantage = new ForVantage();

    public sealed record ForSession(SessionId Id) : TabOwner;

    /// <summary>The tab Vantage itself runs in, so its colour is not handed to a session tab.</summary>
    public sealed record ForVantage : TabOwner;
}