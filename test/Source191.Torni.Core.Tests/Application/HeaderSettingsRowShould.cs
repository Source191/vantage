using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application;

public sealed class HeaderSettingsRowShould
{
    private const int OwnProcess = 4100;

    private static readonly ToolVersion Running = new("0.20.0");
    private static readonly ClaudeCodeVersion Claude = new ClaudeCodeVersion.Reported("2.1.310");

    private readonly FakeProcessRegistry processes = new();
    private readonly InMemoryState state = new();
    private readonly FakeTimeProvider clock = new(Fixtures.Noon);
    private readonly UpdateCheck updates;

    public HeaderSettingsRowShould()
    {
        updates = new UpdateCheck(Running, () => true, state, new FakePackageVersions(["0.20.0"]), clock);
    }

    [Fact]
    public void ShowTheVersionsAndTornisMemory()
    {
        processes.Using(OwnProcess, Fixtures.Noon.AddMinutes(-5), MemorySize.FromMegabytes(35));
        HeaderSettingsRow header = new(Running, Claude, updates, processes, OwnProcess);

        IReadOnlyList<IReadOnlyList<TextPart>> rows = header.Rows(118);

        rows.ShouldHaveSingleItem().ShouldBe(Wording.SettingsRow(Running, Claude, updates.Status, MemorySize.FromMegabytes(35)));
    }

    [Fact]
    public void ShowZeroWhenTheProcessCannotBeMeasured()
    {
        HeaderSettingsRow header = new(Running, Claude, updates, processes, OwnProcess);

        IReadOnlyList<IReadOnlyList<TextPart>> rows = header.Rows(118);

        rows.ShouldHaveSingleItem().ShouldBe(Wording.SettingsRow(Running, Claude, updates.Status, MemorySize.Zero));
    }

    [Fact]
    public async Task ShowTheUpdateStatusAsItIsNow()
    {
        HeaderSettingsRow header = new(Running, Claude, updates, processes, OwnProcess);
        IReadOnlyList<TextPart> before = header.Rows(118).Single();
        ReleaseVersion newer = ReleaseVersions.Version("0.21.0");
        await state.WriteUpdateCheckAsync(new UpdateCheckRecord.Checked(Fixtures.Noon.AddHours(-1), new NewestRelease.Found(newer)));

        await updates.RunAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<TextPart> after = header.Rows(118).Single();

        before.ShouldBe(Wording.SettingsRow(Running, Claude, new UpdateStatus.UpToDate(), MemorySize.Zero));
        after.ShouldBe(Wording.SettingsRow(Running, Claude, new UpdateStatus.Available(ReleaseVersions.Version("0.20.0"), newer), MemorySize.Zero));
    }
}