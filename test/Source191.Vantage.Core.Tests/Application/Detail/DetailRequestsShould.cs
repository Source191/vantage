using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;

namespace Source191.Vantage.Core.Tests.Application.Detail;

public sealed class DetailRequestsShould
{
    [Fact]
    public void NumberEachRequestInTheOrderAsked()
    {
        DetailRequests requests = new();

        DetailRequest first = requests.Ask("fix-123");
        DetailRequest second = requests.Ask("fix-123");

        (first.Number, second.Number).ShouldBe((1, 2));
        second.Key.ShouldBe("fix-123");
    }

    [Fact]
    public void ShowAnAnswerForTheSelectedRow()
    {
        DetailRequests requests = new();
        DetailRequest asked = requests.Ask("fix-123");

        requests.Accept(asked, Content("fix-123")).ShouldNotBeNull().Title.ShouldBe("fix-123");
    }

    [Fact]
    public void DropAnAnswerForARowNoLongerSelected()
    {
        DetailRequests requests = new();
        DetailRequest first = requests.Ask("fix-123");
        requests.Ask("invoice-pdf");

        requests.Accept(first, Content("fix-123")).ShouldBeNull();
    }

    [Fact]
    public void DropTheOldRowsAnswerOnceTheSelectionMoves()
    {
        DetailRequests requests = new();
        DetailRequest first = requests.Ask("fix-123");

        DetailRequest moved = requests.Ask("invoice-pdf");

        requests.Accept(first, Content("fix-123")).ShouldBeNull();
        requests.Accept(moved, Content("invoice-pdf")).ShouldNotBeNull();
    }

    [Fact]
    public void DropAnAnswerOlderThanTheOneShown()
    {
        DetailRequests requests = new();
        DetailRequest older = requests.Ask("Northwind.Orders-fix-123");
        DetailRequest newer = requests.Ask("Northwind.Orders-fix-123");
        requests.Accept(newer, Content("newer")).ShouldNotBeNull();

        requests.Accept(older, Content("older")).ShouldBeNull();
    }

    [Fact]
    public void ShowNothingOnceNoRowIsSelected()
    {
        DetailRequests requests = new();
        DetailRequest asked = requests.Ask("fix-123");

        requests.Cleared();

        requests.Accept(asked, Content("fix-123")).ShouldBeNull();
    }

    private static DetailContent Content(string title) => new(title, [], []);
}