using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class LatestSessionsShould
{
    private const string Claude = @"C:\Users\me\.local\bin\claude.exe";
    private const string Listing = """
        [{"pid":1,"id":"81cdb0f9","cwd":"C:\\Users\\me\\source\\repos\\Northwind.Orders","kind":"background","startedAt":1,"sessionId":"81cdb0f9-1111","name":"fix-orders","status":"idle","state":"blocked"},
         {"pid":2,"id":"92dec1a0","cwd":"C:\\Users\\me\\source\\repos\\Northwind.Billing","kind":"background","startedAt":2,"sessionId":"92dec1a0-2222","name":"fix-billing","status":"busy","state":"working"}]
        """;

    private readonly FakeProcessRunner processes = new FakeProcessRunner()
        .Answer(request => request.Arguments.Contains("agents"), new ProcessResult(0, Listing, string.Empty));

    private LatestSessions Latest => new(new ClaudeCli(processes, Claude));

    private int AgentsRuns => processes.CapturedRequests.Count(request => request.Arguments.Contains("agents"));

    [Fact]
    public async Task AnswerTheSessionsOfTheLastListing()
    {
        LatestSessions latest = Latest;

        SessionListing listing = await latest.ListAsync(CancellationToken.None);
        IReadOnlyList<Session> sessions = await latest.SessionsAsync(CancellationToken.None);

        sessions.Select(session => session.Name).ShouldBe(["fix-orders", "fix-billing"]);
        sessions.ShouldBe(listing.Sessions);
        AgentsRuns.ShouldBe(1);
    }

    [Fact]
    public async Task WaitForTheFirstListing()
    {
        LatestSessions latest = Latest;

        Task<IReadOnlyList<Session>> waiting = latest.SessionsAsync(CancellationToken.None);
        bool completedBeforeTheListing = waiting.IsCompleted;
        await latest.ListAsync(CancellationToken.None);
        IReadOnlyList<Session> sessions = await waiting;

        completedBeforeTheListing.ShouldBeFalse();
        sessions.Select(session => session.Name).ShouldBe(["fix-orders", "fix-billing"]);
    }

    [Fact]
    public async Task AnswerNoSessionsWhenTheFirstListingFails()
    {
        LatestSessions latest = Latest;
        processes.Fail(new InvalidOperationException("claude could not be started"));

        await Should.ThrowAsync<InvalidOperationException>(() => latest.ListAsync(CancellationToken.None));
        IReadOnlyList<Session> sessions = await latest.SessionsAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        sessions.ShouldBeEmpty();
    }

    [Fact]
    public async Task KeepTheLastSessionsWhenALaterListingFails()
    {
        LatestSessions latest = Latest;
        await latest.ListAsync(CancellationToken.None);
        processes.Fail(new InvalidOperationException("claude could not be started"));

        await Should.ThrowAsync<InvalidOperationException>(() => latest.ListAsync(CancellationToken.None));
        IReadOnlyList<Session> sessions = await latest.SessionsAsync(CancellationToken.None);

        sessions.Select(session => session.Name).ShouldBe(["fix-orders", "fix-billing"]);
    }
}