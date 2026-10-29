using Source191.Vantage.Adapters.Diagnostics;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class DiagnosticFileShould : IDisposable
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"vantage-log-{Guid.NewGuid():N}.log");

    public void Dispose()
    {
        File.Delete(path);
    }

    [Fact]
    public void AppendEachLineWithoutAByteOrderMark()
    {
        DiagnosticFile file = new(path);

        file.Write("2026-10-29T12:00:00.000Z view Sessions");
        file.Write("2026-10-29T12:00:01.000Z hand-off returned");

        File.ReadAllLines(path).ShouldBe(["2026-10-29T12:00:00.000Z view Sessions", "2026-10-29T12:00:01.000Z hand-off returned"]);
        File.ReadAllBytes(path)[0].ShouldBe((byte)'2');
    }

    [Fact]
    public void NotThrowWhenTheFileIsLocked()
    {
        DiagnosticFile file = new(path);
        using FileStream locked = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Should.NotThrow(() => file.Write("2026-10-29T12:00:00.000Z view Sessions"));
    }
}