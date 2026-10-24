using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Adapters.Diagnostics;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class FileLoggerShould : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"vantage-log-{Guid.NewGuid():N}.log");

    public void Dispose()
    {
        File.Delete(path);
    }

    [Fact]
    public void AppendOneTimestampedLinePerEntry()
    {
        FakeTimeProvider clock = new(new DateTimeOffset(2026, 10, 23, 17, 5, 9, TimeSpan.Zero));
        FileLogger logger = new(path, clock);

        logger.Log(LogLevel.Information, new EventId(1), "child exited", null, (state, _) => state);
        logger.Log(LogLevel.Warning, new EventId(2), "late reply ESC[?0u", null, (state, _) => state);

        string[] lines = File.ReadAllLines(path);
        lines.Length.ShouldBe(2);
        lines[0].ShouldBe("17:05:09.000 Information child exited");
        lines[1].ShouldBe("17:05:09.000 Warning     late reply ESC[?0u");
    }

    [Fact]
    public void BeEnabledForEveryLevelExceptNone()
    {
        FileLogger logger = new(path, TimeProvider.System);

        logger.IsEnabled(LogLevel.Trace).ShouldBeTrue();
        logger.IsEnabled(LogLevel.None).ShouldBeFalse();
    }
}