using Source191.Vantage.Application;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class RunningLoadsShould
{
    private readonly RunningLoads loads = new();

    [Fact]
    public void ReserveOnlyWhenNothingRuns()
    {
        bool first = loads.TryReserve();
        bool second = loads.TryReserve();

        first.ShouldBeTrue();
        second.ShouldBeFalse();
        loads.IsRunning.ShouldBeTrue();
    }
}