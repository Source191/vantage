using Microsoft.Extensions.Time.Testing;
using Source191.Vantage.Application;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class HeaderSettingsRowShould
{
    private const int VantageProcess = 4100;

    private static readonly DateTimeOffset Noon = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly ToolVersion Running = new("0.20.0");
    private static readonly ClaudeCodeVersion Claude = new ClaudeCodeVersion.Reported("2.1.310");

    private readonly FakeProcessRegistry processes = new();
    private readonly InMemoryState state = new();
    private readonly FakeTimeProvider clock = new(Noon);
    private readonly UpdateCheck updates;

    public HeaderSettingsRowShould()
    {
        updates = new UpdateCheck(Running, () => true, state, new FakePackageVersions(["0.20.0"]), clock);
    }

    [Fact]
    public void ShowTheVersionsAndVantagesMemory()
    {
        processes.Using(VantageProcess, Noon.AddMinutes(-5), MemorySize.FromMegabytes(35));
        HeaderSettingsRow header = new(Running, Claude, updates, processes, VantageProcess);

        IReadOnlyList<IReadOnlyList<TextPart>> rows = header.Rows(118);

        rows.ShouldHaveSingleItem().ShouldBe(Wording.SettingsRow(Running, Claude, updates.Status, MemorySize.FromMegabytes(35)));
    }
}
