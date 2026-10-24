using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class DurationsShould
{
    [Fact]
    public void FormatAges()
    {
        Durations.Age(TimeSpan.FromMinutes(45)).ShouldBe("45m");
        Durations.Age(new TimeSpan(3, 5, 0)).ShouldBe("3h05m");
        Durations.Age(new TimeSpan(2, 4, 30, 0)).ShouldBe("2d4h");
        Durations.Age(TimeSpan.FromMinutes(-5)).ShouldBe("0m");
    }
}