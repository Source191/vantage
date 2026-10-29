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

    [Fact]
    public void EndEveryKeyBy112AndEveryWordBy123()
    {
        foreach ((int keyX, int keyWidth, int hintX, int hintWidth, int _) in KeyHintColumns.Layout)
        {
            (keyX + keyWidth - 1).ShouldBeLessThanOrEqualTo(112);
            (hintX + hintWidth - 1).ShouldBeLessThanOrEqualTo(123);
        }
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void OverlapNoColumnOnAnyRow(string view, IReadOnlyList<KeyHelp> keys)
    {
        (int Left, int Right, int Top, int Bottom)[] areas = [.. KeyHintColumns.Layout.Select((column, index) => (
            Left: column.KeyX,
            Right: column.HintX + column.HintWidth - 1,
            Top: column.FirstRow,
            Bottom: column.FirstRow + keys.Count(key => key.Hint is not null && key.Column == index) - 1))];

        foreach ((int Left, int Right, int Top, int Bottom) first in areas)
        {
            foreach ((int Left, int Right, int Top, int Bottom) second in areas.Where(area => area != first))
            {
                bool overlaps = first.Left <= second.Right && second.Left <= first.Right && first.Top <= second.Bottom && second.Top <= first.Bottom;
                overlaps.ShouldBeFalse($"{view}: columns at {first.Left} and {second.Left} share cells");
            }
        }
    }

    [Fact]
    public void StartTheThirdColumnBelowTheLogo()
    {
        KeyHintColumns.Layout[2].FirstRow.ShouldBe(2, "the logo takes rows 0 and 1 at the right edge");
    }
}