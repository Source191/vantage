using Source191.Vantage.Domain;
using System.Diagnostics;

namespace Source191.Vantage.Adapters.State;

/// <summary>
/// A tab registration as its file holds it: the owner under the "sessionId" key it has always had, and the claiming
/// process and its start time, both null while the tab is pending. A file holding only one of the two reads as pending.
/// </summary>
internal sealed record TabFile(Guid Id, TabOwner SessionId, TabColour Colour, DateTimeOffset CreatedAt, int? ProcessId, DateTimeOffset? ProcessStartedAt)
{
    public static TabFile From(TabRegistration registration) => registration.Claim switch
    {
        TabClaim.Claimed claimed => new(registration.Id, registration.Owner, registration.Colour, registration.CreatedAt, claimed.ProcessId, claimed.StartedAt),
        TabClaim.Pending => new(registration.Id, registration.Owner, registration.Colour, registration.CreatedAt, ProcessId: null, ProcessStartedAt: null),
        _ => throw new UnreachableException(),
    };

    public TabRegistration ToRegistration() => new(Id, SessionId, Colour, CreatedAt, ClaimOf(ProcessId, ProcessStartedAt));

    private static TabClaim ClaimOf(int? processId, DateTimeOffset? startedAt) => (processId, startedAt) is (int process, DateTimeOffset started)
        ? new TabClaim.Claimed(process, started)
        : new TabClaim.Pending();
}