using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

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

    [Fact]
    public void RefuseANullFirstValue()
    {
        Should.Throw<ArgumentNullException>(() => new SharedSnapshot<string>(null!));
    }

    [Fact]
    public void RefuseANullReplacement()
    {
        SharedSnapshot<string> snapshot = new("first");

        Should.Throw<ArgumentNullException>(() => snapshot.Replace(null!));
        snapshot.Value.ShouldBe("first");
    }
}
