using Source191.Vantage.Application;
using Source191.Vantage.Tui;

namespace Source191.Vantage.Core.Tests.Tui;

/// <summary>A key added to a view's list must have a header column to sit in and a hint that fits it.</summary>
public sealed class KeyHintColumnsShould
{
    public static TheoryData<string, IReadOnlyList<KeyHelp>> Views => new()
    {
        { "sessions", Wording.SessionsKeys },
        { "worktrees", Wording.WorktreesKeys },
    };

    [Theory]
    [MemberData(nameof(Views))]
    public void HaveAColumnAndRoomForEveryHint(string view, IReadOnlyList<KeyHelp> keys)
    {
        foreach (KeyHelp key in keys.Where(key => key.Hint is not null))
        {
            key.Column.ShouldBeInRange(0, KeyHintColumns.Layout.Length - 1, $"{view} key {key.Key}");
            (key.Hint!.Length <= KeyHintColumns.Layout[key.Column].HintWidth).ShouldBeTrue($"{view} hint {key.Hint}");
            (key.HeaderKey.Length <= KeyHintColumns.Layout[key.Column].KeyWidth).ShouldBeTrue($"{view} key {key.Key}");
        }
    }
}