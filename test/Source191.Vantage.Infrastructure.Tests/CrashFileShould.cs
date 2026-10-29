using Source191.Vantage.Adapters.Diagnostics;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class CrashFileShould : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), $"vantage-state-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void WriteTheCrashIntoTheStateFolder()
    {
        CrashFile file = new(folder);

        file.Write("2026-10-29T12:00:00.000Z failed Crash: InvalidOperationException\n");

        File.ReadAllText(Path.Combine(folder, "last-crash.log")).ShouldBe("2026-10-29T12:00:00.000Z failed Crash: InvalidOperationException\n");
        File.ReadAllBytes(Path.Combine(folder, "last-crash.log"))[0].ShouldBe((byte)'2');
    }

    [Fact]
    public void ReplaceAnEarlierCrashWhole()
    {
        CrashFile file = new(folder);

        file.Write("first crash with a long line\n");
        file.Write("second\n");

        File.ReadAllText(Path.Combine(folder, "last-crash.log")).ShouldBe("second\n");
    }
}