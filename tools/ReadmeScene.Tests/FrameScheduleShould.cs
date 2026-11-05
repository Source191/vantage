namespace Source191.Torni.ReadmeScene.Tests;

public sealed class FrameScheduleShould
{
    [Fact]
    public void TakeAFrameEveryHundredMilliseconds()
    {
        FrameSchedule schedule = new(TimeSpan.FromMilliseconds(100), TimeSpan.Zero);

        schedule.Offsets(TimeSpan.FromMilliseconds(350)).ShouldBe(
            [TimeSpan.Zero, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300)]);
    }

    [Fact]
    public void StopAfterTheLastKeyAndHold()
    {
        FrameSchedule schedule = new(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));

        schedule.Offsets(TimeSpan.FromMilliseconds(300))[^1].ShouldBe(TimeSpan.FromMilliseconds(500));
    }
}