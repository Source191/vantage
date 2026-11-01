using Source191.Vantage.Application.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class DurationSampleShould
{
    [Fact]
    public void TakeTheMedianAndNinetyFifthPercentileByNearestRank()
    {
        DurationSample sample = new();

        foreach (int _ in Enumerable.Range(0, 99))
        {
            sample.Add(TimeSpan.FromMilliseconds(1600));
        }

        sample.Add(TimeSpan.FromMilliseconds(4900));

        sample.Figures().ShouldBe(new DurationFigures(100, TimeSpan.FromMilliseconds(1600), TimeSpan.FromMilliseconds(1600), TimeSpan.FromMilliseconds(4900)));
    }

    [Fact]
    public void RankTheLatestRunsInOrder()
    {
        DurationSample sample = new();

        foreach (int milliseconds in Enumerable.Range(1, 20).Reverse())
        {
            sample.Add(TimeSpan.FromMilliseconds(milliseconds * 10));
        }

        sample.Figures().ShouldBe(new DurationFigures(20, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(190), TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void KeepTheLatestThousandButCountAndLongestOverEveryRun()
    {
        DurationSample sample = new();

        sample.Add(TimeSpan.FromMilliseconds(9000));
        foreach (int _ in Enumerable.Range(0, 1000))
        {
            sample.Add(TimeSpan.FromMilliseconds(40));
        }

        sample.Figures().ShouldBe(new DurationFigures(1001, TimeSpan.FromMilliseconds(40), TimeSpan.FromMilliseconds(40), TimeSpan.FromMilliseconds(9000)));
    }

    [Fact]
    public void HaveNoFiguresBeforeAnyRun()
    {
        new DurationSample().Figures().ShouldBe(DurationFigures.None);
    }
}
