using Source191.Torni.Application.Actions;
using Source191.Torni.Domain;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Actions;

public sealed class SessionTabsShould
{
    private static readonly SessionId Session = SessionId.From("sess-1");

    private readonly InMemoryState store = new();
    private readonly FakeProcessRegistry processes = new FakeProcessRegistry().Running(4000, Fixtures.Noon.AddMinutes(-5)).Running(4100, Fixtures.Noon.AddMinutes(-2));

    private SessionTabs Tabs => new(store, processes);

    [Fact]
    public async Task EndEveryLiveShellAndForgetTheRegistrations()
    {
        await store.WriteTabAsync(Registration(Session, Fixtures.Noon.AddMinutes(-6)).ClaimedBy(4000, Fixtures.Noon.AddMinutes(-5)));
        await store.WriteTabAsync(Registration(Session, Fixtures.Noon.AddMinutes(-3)).ClaimedBy(4100, Fixtures.Noon.AddMinutes(-2)));

        TabClosure closure = await Tabs.CloseAsync(Session);

        closure.ShouldBe(TabClosure.Closed);
        processes.EndedCleanly.ShouldBe([4000, 4100], ignoreOrder: true);
        (await store.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task ForgetAPendingRegistrationWithoutEndingAnything()
    {
        await store.WriteTabAsync(Registration(Session, Fixtures.Noon.AddSeconds(-10)));

        TabClosure closure = await Tabs.CloseAsync(Session);

        closure.ShouldBe(TabClosure.NoTab);
        processes.EndedCleanly.ShouldBeEmpty();
        (await store.ReadTabsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerNoTabWhenThereIsNone()
    {
        TabRegistration another = Registration(SessionId.From("sess-2"), Fixtures.Noon.AddMinutes(-6)).ClaimedBy(4000, Fixtures.Noon.AddMinutes(-5));
        await store.WriteTabAsync(another);

        TabClosure closure = await Tabs.CloseAsync(Session);

        closure.ShouldBe(TabClosure.NoTab);
        processes.EndedCleanly.ShouldBeEmpty();
        (await store.ReadTabsAsync()).ShouldBe([another]);
    }

    private static TabRegistration Registration(SessionId session, DateTimeOffset createdAt) => TabRegistration.Pending(new TabOwner.ForSession(session), TabPalette.Default[0], createdAt);
}