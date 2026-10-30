using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class SessionListingShould
{
    private static readonly FolderPath Orders = FolderPath.From(@"C:\Users\me\source\worktrees\Northwind.Orders-fix-123");

    [Fact]
    public void KeepTheSessionsAndTheAutomatedRunsApart()
    {
        BackgroundSession fix = new(SessionId.From("2f1c0a9e-1111-4c4c-9a9a-000000000001"), JobId.From("2f1c0a9e"), "fix-123", Orders, DateTimeOffset.UnixEpoch, DisplayState.Working, ProcessId: 4100);
        AutomatedRun review = new(SessionId.From("0fe9bf3e-385c-452d-a37b-000000000002"), Orders, ProcessId: 4200);

        SessionListing listing = new([fix], [review]);

        listing.Sessions.ShouldBe([fix]);
        listing.AutomatedRuns.ShouldBe([review]);
    }

    [Fact]
    public void HoldNeitherWhenEmpty()
    {
        SessionListing.Empty.Sessions.ShouldBeEmpty();
        SessionListing.Empty.AutomatedRuns.ShouldBeEmpty();
    }
}
