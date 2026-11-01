using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ConfigRowsShould
{
    private const string Repos = @"C:\Users\me\source\repos";
    private const string SettingsFile = @"C:\Users\me\AppData\Local\Vantage\config.json";
    private const string Fragment = @"C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json";

    private static readonly SetupFacts AllSetUp = new(
        new SetupState.At(SettingsFile),
        new SetupState.At(Fragment),
        new SetupState.Enabled(),
        new SetupState.Disabled(),
        new SetupState.Checked(new DateTimeOffset(2026, 10, 30, 9, 12, 0, TimeSpan.Zero), new NewestRelease.Found(ReleaseVersions.Version("0.18.0"))));

    [Fact]
    public void ListTheDefaultSettingsFirstInTheirOrder()
    {
        IReadOnlyList<ConfigRow> rows = ConfigRows.For(TestSettings.For(Repos), AllSetUp);

        rows.Take(6).ShouldBe(
        [
            new ConfigRow.RepositoryRoot(0, FolderPath.From(Repos)),
            new ConfigRow.WorktreeRoot(FolderPath.From(@"C:\Users\me\source\worktrees")),
            new ConfigRow.OpenFolderWith(EditorChoice.VsCode),
            new ConfigRow.DefaultBase(WorktreeBase.RemoteDefault),
            new ConfigRow.DefaultPlace(LaunchMode.NewWorktree),
            new ConfigRow.CheckForUpdates(On: true),
        ]);
    }

    [Fact]
    public void GiveEachRootARowInOrder()
    {
        IReadOnlyList<ConfigRow> rows = ConfigRows.For(TestSettings.For(Repos, @"D:\work"), AllSetUp);

        rows.OfType<ConfigRow.RepositoryRoot>().ShouldBe([new ConfigRow.RepositoryRoot(0, FolderPath.From(Repos)), new ConfigRow.RepositoryRoot(1, FolderPath.From(@"D:\work"))]);
        rows[1].ShouldBeOfType<ConfigRow.RepositoryRoot>();
    }

    [Fact]
    public void CarryACustomEditor()
    {
        EditorChoice subl = EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"]);
        Settings settings = Settings.Resolve([FolderPath.From(Repos)], worktreeRoot: null, subl, defaultBase: null, TestSettings.Home);

        ConfigRows.For(settings, AllSetUp).OfType<ConfigRow.OpenFolderWith>().Single().Editor.ShouldBe(subl);
    }

    [Fact]
    public void KeepTheKeysStableAcrossASave()
    {
        IReadOnlyList<ConfigRow> before = ConfigRows.For(TestSettings.For(Repos), AllSetUp);
        IReadOnlyList<ConfigRow> after = ConfigRows.For(TestSettings.For([Repos], @"D:\worktrees"), AllSetUp);

        after.Select(row => row.Key).ShouldBe(before.Select(row => row.Key));
        before.Select(row => row.Key).Distinct().Count().ShouldBe(before.Count);
    }

    [Fact]
    public void ListTheSetupAfterTheSettings()
    {
        IReadOnlyList<ConfigRow> rows = ConfigRows.For(TestSettings.For(Repos), AllSetUp);

        rows.Skip(6).ShouldBe(
        [
            new ConfigRow.Setup(SetupItem.SettingsFile, new SetupState.At(SettingsFile)),
            new ConfigRow.Setup(SetupItem.TerminalProfiles, new SetupState.At(Fragment)),
            new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.Enabled()),
            new ConfigRow.Setup(SetupItem.DiagnosticLog, new SetupState.Disabled()),
            new ConfigRow.Setup(SetupItem.LastUpdateCheck, AllSetUp.LastUpdateCheck),
        ]);
    }

    [Fact]
    public void ShowNothingSetUp()
    {
        SetupFacts nothing = AllSetUp with { TerminalProfiles = new SetupState.NotSetUp(), ClaudeBell = new SetupState.NotSetUp() };

        IReadOnlyList<ConfigRow.Setup> setup = [.. ConfigRows.For(TestSettings.For(Repos), nothing).OfType<ConfigRow.Setup>()];

        setup.Single(row => row.Item == SetupItem.TerminalProfiles).State.ShouldBe(new SetupState.NotSetUp());
        setup.Single(row => row.Item == SetupItem.ClaudeBell).State.ShouldBe(new SetupState.NotSetUp());
    }

    [Fact]
    public void ShowTheSetupAsReadingBeforeItIsRead()
    {
        IReadOnlyList<ConfigRow.Setup> setup = [.. ConfigRows.For(TestSettings.For(Repos), SetupFacts.Reading).OfType<ConfigRow.Setup>()];

        setup.Select(row => row.Item).ShouldBe(Enum.GetValues<SetupItem>());
        setup.ShouldAllBe(row => row.State is SetupState.Reading);
    }

    [Fact]
    public void ShowTheLastUpdateCheckAsOffWhenTheCheckIsOff()
    {
        Settings off = TestSettings.For(Repos).WithUpdateCheckToggled();

        ConfigRows.For(off, AllSetUp).OfType<ConfigRow.Setup>().Single(row => row.Item == SetupItem.LastUpdateCheck).State.ShouldBe(new SetupState.Disabled());
    }
}