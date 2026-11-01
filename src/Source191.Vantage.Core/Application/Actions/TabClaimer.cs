using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.Actions;

/// <summary>What "vantage claim-tab" does inside the new tab: stamps the registration with the tab shell's process.</summary>
public sealed class TabClaimer(ITabRegistry tabs, IProcessRegistry processes)
{
    public async Task ClaimAsync(Guid registrationId, int processId)
    {
        TabRegistration? pending = (await tabs.ReadTabsAsync()).FirstOrDefault(registration => registration.Id == registrationId);
        DateTimeOffset? startedAt = processes.StartTimeOf(processId);
        if (pending is null || startedAt is null)
        {
            return;
        }

        await tabs.WriteTabAsync(pending.ClaimedBy(processId, startedAt.Value));
    }
}