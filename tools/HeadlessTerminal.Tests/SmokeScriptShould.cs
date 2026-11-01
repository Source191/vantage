using Source191.Vantage.Application;

namespace Source191.Vantage.HeadlessTerminal.Tests;

public sealed class SmokeScriptShould
{
    private const string Worktree = "Northwind.Smoke-smoke";
    private const string Escape = "\u001b";
    private const string F3 = "\u001bOR";
    private const string Down = "\u001b[B";

    private static readonly IReadOnlyList<SmokeStep> Steps = SmokeScript.Build(Worktree);

    [Fact]
    public void WaitForEachDialogsTabTitleAndForTheMainTitleAfterItCloses()
    {
        string[] titles = [.. Steps.Select(step => step.Marker).OfType<CaptureMarker.Title>().Select(title => title.Text)];

        titles.ShouldBe(
        [
            Wording.TabTitleDuring(DialogName.Keys), Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.NewSession), Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.ProblemReport), Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.Settings), Wording.TabTitleDuring(DialogName.FolderPicker), Wording.TabTitleDuring(DialogName.Settings), Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.Settings), Wording.TabTitleDuring(DialogName.FilePicker), Wording.TabTitleDuring(DialogName.Settings), Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.Keys), Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.ForceRemove), Wording.TabTitle,
            Wording.TabTitle,
            Wording.TabTitleDuring(DialogName.OpenSolution), Wording.TabTitle,
        ]);
    }

    [Fact]
    public void RecogniseEachViewByAColumnOnlyThatViewHas()
    {
        string[] sessions = [.. Wording.SessionsColumns.Select(column => column.Name)];
        string[] worktrees = [.. Wording.WorktreesColumns.Select(column => column.Name)];
        string[] texts = [.. Steps.Select(step => step.Marker).OfType<CaptureMarker.ScreenText>().Select(text => text.Text)];

        texts.Length.ShouldBe(6);
        texts[0].ShouldBeOneOf(sessions);
        texts[0].ShouldNotBeOneOf(worktrees);
        texts[1].ShouldBe("SETTING", "the Config tab is recognised by a column neither other view has");
        texts[2].ShouldBeOneOf(worktrees);
        texts[2].ShouldNotBeOneOf(sessions);
        texts[3].ShouldBe(", selected: ", "the scan has finished once the table's title names a selection; the WORKTREE cell no longer shows the folder name");
        texts[4].ShouldBe($"New session in {Worktree}", "the fixed-folder wizard is recognised by its own title");
        texts[5].ShouldBe(texts[0]);
    }

    [Fact]
    public void SendTheKeysWordingGivesForEachHintInOrder()
    {
        string[] keys = [.. Steps.Select(step => step.Keys)];

        keys.ShouldBe(
        [
            Key(Wording.SessionsKeys, "Help"), Escape,
            Key(Wording.SessionsKeys, "New session"), Escape,
            Key(Wording.SessionsKeys, "Report"), Escape,
            "3", Key(Wording.ConfigKeys, "Edit"), F3, Escape, Escape,
            Key(Wording.ConfigKeys, "Filter") + "Open folder with", "\r", Key(Wording.ConfigKeys, "Edit"), Down + Down + Down + Down + " " + F3, Escape, Escape,
            "2", string.Empty,
            Key(Wording.WorktreesKeys, "Help"), Escape,
            Key(Wording.WorktreesKeys, "Filter") + Worktree, "\r",
            Key(Wording.WorktreesKeys, "Force remove"), Escape,
            Key(Wording.WorktreesKeys, "New session"), Escape,
            Key(Wording.WorktreesKeys, "Open solution"), Escape,
            "1",
            Key(Wording.SessionsKeys, "Quit"),
        ]);
    }

    [Fact]
    public void SendEnterAndDForceRemoveWithoutAMarkerOfTheirOwn()
    {
        Steps[22].Marker.ShouldBe(CaptureMarker.Nothing);
        Steps[23].Marker.ShouldBe(CaptureMarker.Nothing);
    }

    [Fact]
    public void EndTheConfigFilterAndEditTheRowWithoutAMarkerOfTheirOwn()
    {
        Steps[12].Marker.ShouldBe(CaptureMarker.Nothing);
        Steps[13].Marker.ShouldBe(CaptureMarker.Nothing);
    }

    [Fact]
    public void ChooseCustomTheLastEditorBeforeBrowsingForAFile()
    {
        Enum.GetValues<Source191.Vantage.Domain.EditorPreset>()[^1].ShouldBe(Source191.Vantage.Domain.EditorPreset.Custom, "the smoke reaches Custom by Down presses from the first choice");
        Steps[14].Keys.ShouldEndWith(" " + F3);
    }

    [Fact]
    public void AllowAMinuteForTheColdStartAndTheWorktreeScanAndTwentySecondsForTheRest()
    {
        TimeSpan[] timeouts = [.. Steps.Select(step => step.Timeout)];

        timeouts[0].ShouldBe(TimeSpan.FromSeconds(60));
        timeouts[19].ShouldBe(TimeSpan.FromSeconds(60));
        timeouts.Where((_, index) => index is not 0 and not 19).ShouldAllBe(timeout => timeout == TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void TranslateTheNamedKeysToWhatATerminalSends()
    {
        SmokeScript.KeyFor([new KeyHelp("esc", "Back", "back", Tip: "Go back.")], "Back").ShouldBe(Escape);
        SmokeScript.KeyFor([new KeyHelp("enter", "Go", "go", Tip: "Go there.")], "Go").ShouldBe("\r");
        SmokeScript.KeyFor([new KeyHelp(",", "Settings", "settings", Tip: "Edit the settings.")], "Settings").ShouldBe(",");
    }

    [Fact]
    public void ChooseATabByTheKeyTheHelpGivesIt()
    {
        SmokeScript.TabKey(Wording.SessionsKeys, Source191.Vantage.Application.Views.ConsoleTab.Config).ShouldBe("3");
        SmokeScript.TabKey(Wording.ConfigKeys, Source191.Vantage.Application.Views.ConsoleTab.Worktrees).ShouldBe("2");
    }

    [Fact]
    public void TakeTheFirstKeyWithAHint()
    {
        SmokeScript.KeyFor([new KeyHelp("w", "Sessions", "back", Tip: "Go back."), new KeyHelp("esc", "Sessions", "back", Tip: "Go back.")], "Sessions").ShouldBe("w");
    }

    [Fact]
    public void NameTheHintItCouldNotFind()
    {
        InvalidOperationException missing = Should.Throw<InvalidOperationException>(() => SmokeScript.KeyFor(Wording.SessionsKeys, "Teleport"));

        missing.Message.ShouldContain("\"Teleport\"");
    }

    private static string Key(IReadOnlyList<KeyHelp> keys, string hint)
    {
        string key = keys.First(help => help.Hint == hint).Key;

        return key switch
        {
            "esc" => Escape,
            "enter" => "\r",
            _ => key,
        };
    }
}