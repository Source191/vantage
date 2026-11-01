namespace Source191.Vantage.Domain;

public sealed class TabRegistrations(IReadOnlyList<TabRegistration> items)
{
    public IReadOnlyList<TabRegistration> Live(DateTimeOffset now, IProcessRegistry processes) => [.. items.Where(registration => registration.IsLive(now, processes))];
}