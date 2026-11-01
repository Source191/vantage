using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class LoadSequenceShould
{
    [Fact]
    public void AcceptLoadsInTheOrderTheyBegan()
    {
        LoadSequence sequence = new();

        int first = sequence.Begin();
        int second = sequence.Begin();
        int third = sequence.Begin();

        first.ShouldBe(1);
        second.ShouldBe(2);
        third.ShouldBe(3);
        sequence.Accept(first).ShouldBeTrue();
        sequence.Accept(second).ShouldBeTrue();
        sequence.Accept(third).ShouldBeTrue();
    }

    [Fact]
    public void DropAResultOlderThanTheLastApplied()
    {
        LoadSequence sequence = new();
        int first = sequence.Begin();
        int second = sequence.Begin();

        sequence.Accept(second).ShouldBeTrue();
        sequence.Accept(first).ShouldBeFalse();

        int third = sequence.Begin();

        sequence.Accept(third).ShouldBeTrue();
    }
}