using Source191.Vantage.Application;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.TestDoubles;
using Sessions = Source191.Vantage.Core.Tests.Application.Actions.SessionTestData;
using Worktrees = Source191.Vantage.Core.Tests.Application.Actions.WorktreeTestData;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// A key the help and the key bar list is a key the view answers: pressing it on a loaded table with a row selected and no
/// message either moves the tab or the columns, or changes the state, or asks for an effect.
/// </summary>
public sealed class KeyHelpParityShould
{
    private static readonly SessionsViewState LoadedSessions = SessionsView.Reduce(SessionsViewState.Empty, new RowsLoaded([Sessions.Background(Sessions.Repository, isWorktree: false), Sessions.Interactive("repos-93", 4242)])).State;

    private static readonly WorktreesViewState LoadedWorktrees = WorktreesView.Reduce(WorktreesViewState.Empty, new WorktreeRowsLoaded([Worktrees.Row(WorktreeHealthKind.Ok)])).State;

    private static readonly ConfigViewState LoadedConfig = ConfigView.Reduce(ConfigViewState.Empty, new ConfigRowsLoaded(ConfigRows.For(TestSettings.For(@"C:\Users\me\source\repos"), ConfigFacts()))).State;

    public static TheoryData<string> SessionsKeyTexts => [.. Wording.SessionsKeys.Select(help => help.Key)];

    public static TheoryData<string> WorktreesKeyTexts => [.. Wording.WorktreesKeys.Select(help => help.Key)];

    public static TheoryData<string> ConfigKeyTexts => [.. Wording.ConfigKeys.Select(help => help.Key)];

    [Theory]
    [MemberData(nameof(SessionsKeyTexts))]
    public void AnswerEveryKeyTheSessionsHelpLists(string text)
    {
        Key key = KeyFor(text);

        IsAnswered(key, () => Pressed(SessionsView.Reduce(LoadedSessions, new KeyPressed(key))), LoadedSessions).ShouldBeTrue($"{text} is listed for Sessions and nothing answers it");
    }

    [Theory]
    [MemberData(nameof(WorktreesKeyTexts))]
    public void AnswerEveryKeyTheWorktreesHelpLists(string text)
    {
        Key key = KeyFor(text);

        IsAnswered(key, () => Pressed(WorktreesView.Reduce(LoadedWorktrees, new KeyPressed(key))), LoadedWorktrees).ShouldBeTrue($"{text} is listed for Worktrees and nothing answers it");
    }

    [Theory]
    [MemberData(nameof(ConfigKeyTexts))]
    public void AnswerEveryKeyTheConfigHelpLists(string text)
    {
        Key key = KeyFor(text);

        IsAnswered(key, () => Pressed(ConfigView.Reduce(LoadedConfig, new KeyPressed(key))), LoadedConfig).ShouldBeTrue($"{text} is listed for Config and nothing answers it");
    }

    [Fact]
    public void AnswerTheKeysOnlyTheHelpListsAsTabOrScrollKeys()
    {
        IEnumerable<KeyHelp> helpOnly = Wording.SessionsKeys.Concat(Wording.WorktreesKeys).Concat(Wording.ConfigKeys).Where(help => !help.OnBar);

        foreach (KeyHelp help in helpOnly)
        {
            Key key = KeyFor(help.Key);

            (TabKeys.TryFor(key, out _) || ScrollKeys.TryFor(key, out _)).ShouldBeTrue($"{help.Key} is listed by the help alone and is neither a tab key nor a scroll key");
        }
    }

    private static bool IsAnswered(Key key, Func<(object State, IReadOnlyList<ViewEffect> Effects)> reduce, object before)
    {
        if (TabKeys.TryFor(key, out _) || ScrollKeys.TryFor(key, out _))
        {
            return true;
        }

        (object after, IReadOnlyList<ViewEffect> effects) = reduce();

        return effects.Count > 0 || !Equals(after, before);
    }

    private static (object State, IReadOnlyList<ViewEffect> Effects) Pressed(ViewUpdate update) => (update.State, update.Effects);

    private static (object State, IReadOnlyList<ViewEffect> Effects) Pressed(WorktreesViewUpdate update) => (update.State, update.Effects);

    private static (object State, IReadOnlyList<ViewEffect> Effects) Pressed(ConfigViewUpdate update) => (update.State, update.Effects);
    private static Key KeyFor(string text) => text switch
    {
        "enter" => Key.Enter,
        "esc" => Key.Escape,
        "←" => Key.Left,
        "→" => Key.Right,
        _ => Key.Of(text.Single()),
    };

    private static SetupFacts ConfigFacts() => new(
        new SetupState.At(@"C:\Users\me\AppData\Local\Vantage\config.json"),
        new SetupState.NotSetUp(),
        new SetupState.Enabled(),
        new SetupState.Disabled(),
        new SetupState.Never());
}