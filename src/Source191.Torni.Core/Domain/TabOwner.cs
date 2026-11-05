namespace Source191.Torni.Domain;

/// <summary>Whose tab a registration is: a session's, or the console's own.</summary>
public abstract record TabOwner
{
    public static readonly TabOwner Console = new ForConsole();

    public sealed record ForSession(SessionId Id) : TabOwner;

    /// <summary>The tab the console itself runs in, so its colour is not handed to a session tab.</summary>
    public sealed record ForConsole : TabOwner;
}