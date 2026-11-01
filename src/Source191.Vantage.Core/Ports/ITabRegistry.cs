using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The tabs Vantage has opened, one record each, read with stale ones still present (the caller judges liveness).</summary>
public interface ITabRegistry
{
    Task<IReadOnlyList<TabRegistration>> ReadTabsAsync();

    Task WriteTabAsync(TabRegistration registration);

    Task RemoveTabAsync(Guid id);
}