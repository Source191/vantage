using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application;

public sealed class WordingOfConfigShould
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 15, 0, 0, TimeSpan.Zero);
    private const string Fragment = @"C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json";

    public static TheoryData<ConfigRow, string, string> Rows => new()
    {
        { new ConfigRow.RepositoryRoot(0, FolderPath.From(@"C:\Users\me\source\repos")), "Repository roots", @"C:\Users\me\source\repos" },
        { new ConfigRow.RepositoryRoot(1, FolderPath.From(@"D:\work")), string.Empty, @"D:\work" },
        { new ConfigRow.WorktreeRoot(FolderPath.From(@"C:\Users\me\source\worktrees")), "Worktree root", @"C:\Users\me\source\worktrees" },
        { new ConfigRow.OpenFolderWith(EditorChoice.VsCode), "Open folder with", "VS Code" },
        { new ConfigRow.OpenFolderWith(EditorChoice.Custom(@"C:\Tools\subl.exe", ["-n", "{folder}"])), "Open folder with", @"C:\Tools\subl.exe -n {folder}" },
        { new ConfigRow.DefaultBase(WorktreeBase.RemoteDefault), "Default base", "origin's default branch, as last fetched" },
        { new ConfigRow.DefaultBase(WorktreeBase.Head), "Default base", "the commit checked out now (HEAD)" },
        { new ConfigRow.DefaultPlace(LaunchMode.NewWorktree), "Default place", "New worktree" },
        { new ConfigRow.DefaultPlace(LaunchMode.CurrentBranch), "Default place", "Current branch" },
        { new ConfigRow.CheckForUpdates(On: true), "Check for updates", "on" },
        { new ConfigRow.CheckForUpdates(On: false), "Check for updates", "off" },
        { new ConfigRow.Setup(SetupItem.SettingsFile, new SetupState.At(@"C:\Users\me\AppData\Local\Vantage\config.json")), "Settings file", @"C:\Users\me\AppData\Local\Vantage\config.json" },
        { new ConfigRow.Setup(SetupItem.TerminalProfiles, new SetupState.At(Fragment)), "Windows Terminal profiles", $"installed, {Fragment}" },
        { new ConfigRow.Setup(SetupItem.TerminalProfiles, new SetupState.NotSetUp()), "Windows Terminal profiles", "not set up, run vantage setup-terminal" },
        { new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.Enabled()), "Claude bell", "on" },
        { new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.NotSetUp()), "Claude bell", "not set, run vantage setup-terminal" },
        { new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.OtherChannel("iterm2")), "Claude bell", "iterm2, not the terminal bell" },
        { new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.Unreadable()), "Claude bell", "could not be read" },
        { new ConfigRow.Setup(SetupItem.DiagnosticLog, new SetupState.Disabled()), "Diagnostic log", "off" },
        { new ConfigRow.Setup(SetupItem.DiagnosticLog, new SetupState.Reading()), "Diagnostic log", "reading…" },
        { new ConfigRow.Setup(SetupItem.LastUpdateCheck, new SetupState.Checked(new DateTimeOffset(2026, 10, 30, 9, 12, 0, TimeSpan.Zero), "0.18.0")), "Last update check", "09:12 today, newest 0.18.0" },
        { new ConfigRow.Setup(SetupItem.LastUpdateCheck, new SetupState.Checked(new DateTimeOffset(2026, 10, 28, 18, 5, 0, TimeSpan.Zero), Newest: null)), "Last update check", "28 October 2026 18:05" },
        { new ConfigRow.Setup(SetupItem.LastUpdateCheck, new SetupState.Never()), "Last update check", "never" },
    };

    [Fact]
    public void DescribeEverySettingAndEveryPieceOfSetupDifferently()
    {
        ConfigRow[] rows =
        [
            new ConfigRow.RepositoryRoot(0, FolderPath.From(@"C:\Users\me\source\repos")),
            new ConfigRow.WorktreeRoot(FolderPath.From(@"C:\Users\me\source\worktrees")),
            new ConfigRow.OpenFolderWith(EditorChoice.VsCode),
            new ConfigRow.DefaultBase(WorktreeBase.RemoteDefault),
            new ConfigRow.DefaultPlace(LaunchMode.NewWorktree),
            new ConfigRow.CheckForUpdates(On: true),
            .. Enum.GetValues<SetupItem>().Select(item => new ConfigRow.Setup(item, new SetupState.Reading())),
        ];

        string[] descriptions = [.. rows.Select(Wording.ConfigDescription)];

        descriptions.ShouldAllBe(description => description.Length > 0 && description.Length <= 110);
        descriptions.Distinct().Count().ShouldBe(rows.Length);
    }

    [Fact]
    public void SayWhereWorktreesGoAndWhatTheBellIsFor()
    {
        Wording.ConfigDescription(new ConfigRow.WorktreeRoot(FolderPath.From(@"C:\Users\me\source\worktrees"))).ShouldBe("Where new worktrees go, each in a folder named <repository>-<slug> on a branch named <slug>.");
        Wording.ConfigDescription(new ConfigRow.Setup(SetupItem.ClaudeBell, new SetupState.Enabled())).ShouldBe("Claude's terminal bell, which rings when a session finishes or needs permission while you are away.");
        Wording.ConfigDescription(new ConfigRow.RepositoryRoot(1, FolderPath.From(@"D:\work"))).ShouldBe(Wording.ConfigDescription(new ConfigRow.RepositoryRoot(0, FolderPath.From(@"C:\Users\me\source\repos"))));
    }

    [Theory]
    [MemberData(nameof(Rows))]
    public void NameEachRowAndSayItsValue(ConfigRow row, string setting, string value)
    {
        Wording.ConfigSetting(row).ShouldBe(setting);
        Wording.ConfigValue(row, Now).ShouldBe(value);
    }

    [Fact]
    public void SayHowEachPieceOfSetupIsChanged()
    {
        Wording.Describe(SetupItem.TerminalProfiles).ShouldBe("Run vantage setup-terminal, then restart Windows Terminal, to add the Vantage profiles.");
        Wording.Describe(new Footer.SetupAdvice(SetupItem.DiagnosticLog)).ShouldBe("Set the VANTAGE_LOG environment variable to a file path before starting Vantage to keep a diagnostic log.");
        Enum.GetValues<SetupItem>().Select(Wording.Describe).Distinct().Count().ShouldBe(Enum.GetValues<SetupItem>().Length);
    }

    [Fact]
    public void SayTheConfigFooters()
    {
        Wording.Describe(new Footer.SettingHint(new ConfigRow.CheckForUpdates(On: true))).ShouldBe(Wording.ConfigDescription(new ConfigRow.CheckForUpdates(On: true)));
        Wording.Describe(new Footer.NoPathHere()).ShouldBe("This row's value is not a path.");
    }

    [Fact]
    public void TitleEachSettingsDialog()
    {
        Wording.SettingsDialogTitle(new SettingsScope.Roots()).ShouldBe("Repository roots");
        Wording.SettingsDialogTitle(new SettingsScope.WorktreeRoot()).ShouldBe("Worktree root");
        Wording.SettingsDialogTitle(new SettingsScope.Editor()).ShouldBe("Open folder with");
        Wording.SettingsDialogTitle(new SettingsScope.Base()).ShouldBe("Worktree base");
        Wording.SettingsDialogTitle(new SettingsScope.Place()).ShouldBe("Default place");
    }

    [Fact]
    public void SayTheDefaultBaseIsOriginAsLastFetchedWithNoFetch()
    {
        Wording.ConfigDescription(new ConfigRow.DefaultBase(WorktreeBase.RemoteDefault)).ShouldBe("New worktrees branch from origin's default branch as of your last git fetch (no fetch is made), or from HEAD.");
        Enum.GetValues<WorktreeBase>().Select(Wording.Describe).ShouldBe(["origin's default branch, as last fetched", "the commit checked out now (HEAD)"]);
    }

    [Fact]
    public void NameTheBrowseButtonAndThePicker()
    {
        Wording.BrowseButton.ShouldBe("_Browse… (F3)");
        Wording.FolderPickerTitle(new SettingsScope.Roots()).ShouldBe("Add repository roots");
        Wording.FolderPickerTitle(new SettingsScope.WorktreeRoot()).ShouldBe("Choose the worktree root");
    }

    [Fact]
    public void CaptionThePlaceDialogAndNameEachPlace()
    {
        Wording.PlaceCaption.ShouldBe("Where the wizard opened with n starts a session (↑↓ then Space)");
        Enum.GetValues<LaunchMode>().Select(Wording.Describe).ShouldBe(["Current branch", "New worktree"]);
    }
}