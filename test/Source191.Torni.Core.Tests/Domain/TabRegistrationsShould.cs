using Source191.Torni.Domain;

namespace Source191.Torni.Core.Tests.Domain;

public sealed class TabRegistrationsShould
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void KeepTheRegistrationsOwnedByASession()
    {
        TabRegistration first = Registration(new TabOwner.ForSession(SessionId.From("sess-1")));
        TabRegistration other = Registration(new TabOwner.ForSession(SessionId.From("sess-2")));
        TabRegistration second = Registration(new TabOwner.ForSession(SessionId.From("sess-1")));
        TabRegistration own = Registration(TabOwner.Console);
        TabRegistrations registrations = new([first, other, second, own]);

        registrations.For(SessionId.From("sess-1")).ShouldBe([first, second]);
    }

    private static TabRegistration Registration(TabOwner owner) => TabRegistration.Pending(owner, TabPalette.Default[0], Now);
}