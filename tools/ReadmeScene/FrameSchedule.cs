namespace Source191.Torni.ReadmeScene;

/// <summary>When to take each frame of the animation: one every <paramref name="interval"/> from the first key until <paramref name="hold"/> after the last.</summary>
public sealed class FrameSchedule(TimeSpan interval, TimeSpan hold)
{
    public static readonly FrameSchedule Animation = new(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));

    public TimeSpan Interval => interval;

    public IReadOnlyList<TimeSpan> Offsets(TimeSpan playing)
    {
        long frames = ((playing + hold).Ticks / interval.Ticks) + 1;

        return [.. Enumerable.Range(0, (int)frames).Select(frame => interval * frame)];
    }
}