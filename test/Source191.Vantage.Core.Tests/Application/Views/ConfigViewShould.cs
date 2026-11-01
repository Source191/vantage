using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ConfigViewShould
{
    private const string Repos = @"C:\Users\me\source\repos";
    private const string Work = @"D:\work";
    private const string SettingsFile = @"C:\Users\me\AppData\Local\Vantage\config.json";

    private static readonly SetupFacts Facts = new(
        new SetupState.At(SettingsFile),
        new SetupState.NotSetUp(),
        new SetupState.Enabled(),
        new SetupState.Disabled(),
        new SetupState.Never());

    private static readonly ConfigViewState Loaded = ConfigView.Reduce(ConfigViewState.Empty, new ConfigRowsLoaded(ConfigRows.For(TestSettings.For(Repos, Work), Facts))).State;

    [Fact]
    public void LoadTheRowsWithTheFirstSelected()
    {
        Loaded.Loaded.ShouldBeTrue();
        Loaded.Selected.ShouldBe(new ConfigRow.RepositoryRoot(0, FolderPath.From(Repos)));
        Loaded.Shown.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void ShowALoadFailureAsABannerUntilTheNextLoad()
    {
        ConfigViewState failed = ConfigView.Reduce(Loaded, new LoadFailed("config.json is locked")).State;

        failed.Banner.ShouldBe(new LoadBanner.Failed("config.json is locked"));
        ConfigView.Reduce(failed, new ConfigRowsLoaded(Loaded.Rows)).State.Banner.ShouldBe(new LoadBanner.None());
    }

    [Theory]
    [InlineData("root:1")]
    [InlineData("worktree-root")]
    [InlineData("open-folder-with")]
    [InlineData("default-base")]
    [InlineData("default-place")]
    public void EditASettingOnEnter(string key)
    {
        ConfigViewState state = At(key);

        ConfigView.Reduce(state, new KeyPressed(Key.Enter)).Effects.ShouldBe([new EditSetting(SettingsScope.For(state.Selected!))]);
    }

    [Fact]
    public void ToggleTheUpdateCheckOnEnter()
    {
        ConfigView.Reduce(At("check-for-updates"), new KeyPressed(Key.Enter)).Effects.ShouldBe([new ToggleUpdateCheck()]);
    }

    [Theory]
    [InlineData(SetupItem.SettingsFile)]
    [InlineData(SetupItem.TerminalProfiles)]
    [InlineData(SetupItem.ClaudeBell)]
    [InlineData(SetupItem.DiagnosticLog)]
    [InlineData(SetupItem.LastUpdateCheck)]
    public void AdviseOnASetupRowOnEnter(SetupItem item)
    {
        ConfigViewUpdate update = ConfigView.Reduce(At($"setup:{item}"), new KeyPressed(Key.Enter));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.SetupAdvice(item));
    }

    [Theory]
    [InlineData("root:0")]
    [InlineData("root:1")]
    public void EditEveryRootTogetherFromAnyRootRow(string key)
    {
        ConfigView.Reduce(At(key), new KeyPressed(Key.Enter)).Effects.ShouldBe([new EditSetting(new SettingsScope.Roots())]);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('d')]
    public void LeaveAddingAndRemovingRootsToTheDialog(char key)
    {
        ConfigView.Reduce(At("root:1"), new KeyPressed(Key.Of(key))).Effects.ShouldBeEmpty();
    }

    [Fact]
    public void CopyAPathWhole()
    {
        ConfigView.Reduce(At("root:1"), new KeyPressed(Key.Of('c'))).Effects.ShouldBe([new CopyFolderPath(FolderPath.From(Work))]);
        ConfigView.Reduce(At("setup:SettingsFile"), new KeyPressed(Key.Of('c'))).Effects.ShouldBe([new CopyFolderPath(FolderPath.From(SettingsFile))]);
    }

    [Fact]
    public void OpenAFolderOrTheFolderHoldingAFile()
    {
        ConfigView.Reduce(At("worktree-root"), new KeyPressed(Key.Of('f'))).Effects.ShouldBe([new OpenFolderInEditor(FolderPath.From(@"C:\Users\me\source\worktrees"))]);
        ConfigView.Reduce(At("setup:SettingsFile"), new KeyPressed(Key.Of('f'))).Effects.ShouldBe([new OpenFolderInEditor(FolderPath.From(@"C:\Users\me\AppData\Local\Vantage"))]);
    }

    [Fact]
    public void FilterTheRowsByAPath()
    {
        ConfigViewState filtered = Press(Loaded, Key.Slash, Key.Of('w'), Key.Of('o'), Key.Of('r'), Key.Of('k'), Key.Enter);

        filtered.List.Visible.Select(row => row.Key).ShouldBe(["root:1", "worktree-root"]);
    }

    [Theory]
    [InlineData('c')]
    [InlineData('f')]
    public void SayWhenTheValueIsNotAPath(char key)
    {
        ConfigViewUpdate update = ConfigView.Reduce(At("open-folder-with"), new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.Message.ShouldBe(new StatusMessage.NoPathHere());
    }


    [Fact]
    public void ChooseATabWithTheTabKeys()
    {
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Of('1'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Sessions)]);
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Of('2'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Worktrees)]);
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Of('3'))).Effects.ShouldBe([new SelectTab(ConsoleTab.Config)]);
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Left)).Effects.ShouldBe([new PreviousTab()]);
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Right)).Effects.ShouldBe([new NextTab()]);
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Escape)).Effects.ShouldBe([new SelectTab(ConsoleTab.Sessions)]);
    }

    [Theory]
    [InlineData('w')]
    [InlineData(',')]
    [InlineData('e')]
    [InlineData('v')]
    public void IgnoreTheRetiredKeys(char key)
    {
        ConfigViewUpdate update = ConfigView.Reduce(Loaded, new KeyPressed(Key.Of(key)));

        update.Effects.ShouldBeEmpty();
        update.State.ShouldBe(Loaded);
    }

    [Fact]
    public void ClearAFilterOnEscapeBeforeLeaving()
    {
        ConfigViewState filtered = Press(Loaded, Key.Slash, Key.Of('t'), Key.Of('r'), Key.Of('e'), Key.Of('e'), Key.Enter);

        filtered.Selected.ShouldBe(new ConfigRow.WorktreeRoot(FolderPath.From(@"C:\Users\me\source\worktrees")));
        ConfigViewUpdate update = ConfigView.Reduce(filtered, new KeyPressed(Key.Escape));

        update.Effects.ShouldBeEmpty();
        update.State.Filter.Text.ShouldBeEmpty();
    }

    [Theory]
    [InlineData('q', typeof(Quit))]
    [InlineData('?', typeof(ShowHelp))]
    [InlineData('!', typeof(ShowProblemReport))]
    [InlineData('U', typeof(UpdateVantage))]
    public void EmitTheEffectForTheKey(char key, Type effect)
    {
        ConfigView.Reduce(Loaded, new KeyPressed(Key.Of(key))).Effects.Single().ShouldBeOfType(effect);
    }

    [Fact]
    public void KeepTheSelectionWhenTheRowsReload()
    {
        ConfigViewState state = At("worktree-root");

        ConfigViewState reloaded = ConfigView.Reduce(state, new ConfigRowsLoaded(ConfigRows.For(TestSettings.For([Repos, Work], @"E:\trees"), Facts))).State;

        reloaded.Selected.ShouldBe(new ConfigRow.WorktreeRoot(FolderPath.From(@"E:\trees")));
    }

    [Fact]
    public void MoveTheSelectionAndClearTheMessage()
    {
        ConfigViewState advised = ConfigView.Reduce(At("setup:ClaudeBell"), new KeyPressed(Key.Enter)).State;

        ConfigViewState moved = Press(advised, Key.Down);

        moved.Selected!.Key.ShouldBe("setup:DiagnosticLog");
        moved.Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void ShowNoMessageForTheSelectedRowAlone()
    {
        ConfigViewState onWorktreeRoot = Press(Loaded, Key.Down, Key.Down);
        ConfigViewState advised = ConfigView.Reduce(At("setup:ClaudeBell"), new KeyPressed(Key.Enter)).State;

        onWorktreeRoot.Shown.ShouldBe(StatusMessage.None);
        advised.Shown.ShouldBe(advised.Message);
        ConfigViewState moved = Press(advised, Key.Down);

        moved.Shown.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void ClearAMessageWhenTheFilterChanges()
    {
        ConfigViewState advised = ConfigView.Reduce(At("setup:ClaudeBell"), new KeyPressed(Key.Enter)).State;

        Press(advised, Key.Of('/')).Message.ShouldBe(StatusMessage.None);
        Press(Press(advised, Key.Of('/')) with { Message = new StatusMessage.Kept() }, Key.Of('w')).Message.ShouldBe(StatusMessage.None);
    }

    private static ConfigViewState At(string key) => Loaded.With(Loaded.List.Select(key));

    private static ConfigViewState Press(ConfigViewState state, params Key[] keys) => keys.Aggregate(state, (current, key) => ConfigView.Reduce(current, new KeyPressed(key)).State);

    [Fact]
    public void ClearTheMessageItWasAskedToClear()
    {
        StatusMessage message = new StatusMessage.NoPathHere();
        ConfigViewState shown = Loaded with { Message = message };

        ConfigView.Reduce(shown, new MessageDismissed(message)).State.Message.ShouldBe(StatusMessage.None);
    }

    [Fact]
    public void KeepANewerMessageWithTheSameWords()
    {
        StatusMessage older = new StatusMessage.NoPathHere();
        StatusMessage newer = new StatusMessage.NoPathHere();
        ConfigViewState shown = Loaded with { Message = newer };

        ConfigView.Reduce(shown, new MessageDismissed(older)).State.Message.ShouldBeSameAs(newer);
    }
}