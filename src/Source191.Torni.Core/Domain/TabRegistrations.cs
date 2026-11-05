namespace Source191.Torni.Domain;

public sealed class TabRegistrations(IReadOnlyList<TabRegistration> items)
{
    /// <summary>The registrations a session owns, in the order they are held.</summary>
    public IReadOnlyList<TabRegistration> For(SessionId session)
    {
        TabOwner owner = new TabOwner.ForSession(session);

        return [.. items.Where(registration => registration.Owner == owner)];
    }

    public IReadOnlyList<TabRegistration> Live(DateTimeOffset now, IProcessRegistry processes) => [.. items.Where(registration => registration.IsLive(now, processes))];
}