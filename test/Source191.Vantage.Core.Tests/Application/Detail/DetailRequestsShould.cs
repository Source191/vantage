using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;

namespace Source191.Vantage.Core.Tests.Application.Detail;

public sealed class DetailRequestsShould
{
    private static readonly IReadOnlyList<DetailLine> Screen = [new(string.Empty, "Allow edit to Processor.cs?")];

    [Fact]
    public void AskForTheFactsAtOnceAndTheScreenBehindThemWhenTheSelectionMoves()
    {
        DetailRequests requests = new(readsScreen: true);

        IReadOnlyList<DetailRequest> asked = requests.Selected("fix-123");

        asked.Select(request => request.ReadScreen).ShouldBe([false, true]);
        asked.Select(request => request.Number).ShouldBe([1, 2]);
    }

    [Fact]
    public void AskForTheFactsAloneOnATabWithoutAScreen()
    {
        DetailRequests requests = new(readsScreen: false);

        requests.Selected("Northwind.Orders-fix-123").Select(request => request.ReadScreen).ShouldBe([false]);
        requests.Refreshed("Northwind.Orders-fix-123").Select(request => request.ReadScreen).ShouldBe([false]);
    }

    [Fact]
    public void ReadTheScreenEveryOtherRefresh()
    {
        DetailRequests requests = new(readsScreen: true);
        requests.Selected("fix-123");

        IEnumerable<bool> reads = Enumerable.Range(0, 4).Select(_ => requests.Refreshed("fix-123").Single().ReadScreen);

        reads.ShouldBe([false, true, false, true]);
    }

    [Fact]
    public void TreatARefreshThatMovedTheSelectionAsAMove()
    {
        DetailRequests requests = new(readsScreen: true);
        requests.Selected("fix-123");

        requests.Refreshed("invoice-pdf").Select(request => request.ReadScreen).ShouldBe([false, true]);
    }

    [Fact]
    public void SayTheScreenIsBeingReadUntilItArrives()
    {
        DetailRequests requests = new(readsScreen: true);
        DetailRequest facts = requests.Selected("fix-123")[0];

        DetailContent? shown = requests.Accept(facts, Content("facts", below: null));

        shown.ShouldNotBeNull().Below.ShouldBe(Wording.ScreenBeingRead);
    }

    [Fact]
    public void ShowTheScreenWhenItArrivesAndKeepItWhileItIsNotRead()
    {
        DetailRequests requests = new(readsScreen: true);
        IReadOnlyList<DetailRequest> asked = requests.Selected("fix-123");
        requests.Accept(asked[0], Content("facts", below: null));
        requests.Accept(asked[1], Content("facts", Screen)).ShouldNotBeNull().Below.ShouldBe(Screen);

        DetailContent? refreshed = requests.Accept(requests.Refreshed("fix-123").Single(), Content("newer facts", below: null));

        refreshed.ShouldNotBeNull().Title.ShouldBe("newer facts");
        refreshed.Below.ShouldBe(Screen);
    }

    [Fact]
    public void DropAnAnswerForARowNoLongerSelected()
    {
        DetailRequests requests = new(readsScreen: true);
        IReadOnlyList<DetailRequest> first = requests.Selected("fix-123");
        requests.Selected("invoice-pdf");

        requests.Accept(first[1], Content("fix-123", Screen)).ShouldBeNull();
    }

    [Fact]
    public void NeverShowAnotherRowsScreen()
    {
        DetailRequests requests = new(readsScreen: true);
        IReadOnlyList<DetailRequest> first = requests.Selected("fix-123");
        requests.Accept(first[1], Content("fix-123", Screen));

        DetailContent? moved = requests.Accept(requests.Selected("invoice-pdf")[0], Content("invoice-pdf", below: null));

        moved.ShouldNotBeNull().Below.ShouldBe(Wording.ScreenBeingRead);
    }

    [Fact]
    public void DropFactsOlderThanTheFactsShown()
    {
        DetailRequests requests = new(readsScreen: false);
        DetailRequest older = requests.Selected("Northwind.Orders-fix-123").Single();
        DetailRequest newer = requests.Refreshed("Northwind.Orders-fix-123").Single();
        requests.Accept(newer, Content("newer", below: [])).ShouldNotBeNull();

        requests.Accept(older, Content("older", below: [])).ShouldBeNull();
    }

    [Fact]
    public void KeepTheFactsShownWhenAnOlderScreenArrivesAfterNewerFacts()
    {
        DetailRequests requests = new(readsScreen: true);
        IReadOnlyList<DetailRequest> asked = requests.Selected("fix-123");
        DetailRequest refresh = requests.Refreshed("fix-123").Single();
        requests.Accept(refresh, Content("newer", below: null));

        DetailContent? shown = requests.Accept(asked[1], Content("older", Screen));

        shown.ShouldNotBeNull().Title.ShouldBe("newer");
        shown.Below.ShouldBe(Screen);
    }

    [Fact]
    public void ShowNothingOnceNoRowIsSelected()
    {
        DetailRequests requests = new(readsScreen: true);
        IReadOnlyList<DetailRequest> asked = requests.Selected("fix-123");

        requests.Cleared();

        requests.Accept(asked[0], Content("fix-123", below: null)).ShouldBeNull();
    }

    private static DetailContent Content(string title, IReadOnlyList<DetailLine>? below) => new(title, [], below, KeepsLastLines: true);
}