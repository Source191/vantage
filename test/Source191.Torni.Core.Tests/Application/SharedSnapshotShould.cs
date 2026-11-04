using Source191.Torni.Application;

namespace Source191.Torni.Core.Tests.Application;

public sealed class SharedSnapshotShould
{
    [Fact]
    public void AnswerTheFirstValue()
    {
        SharedSnapshot<string> snapshot = new("first");

        snapshot.Value.ShouldBe("first");
    }

    [Fact]
    public void AnswerTheReplacement()
    {
        SharedSnapshot<string> snapshot = new("first");

        snapshot.Replace("second");

        snapshot.Value.ShouldBe("second");
    }
}