using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ListSelectionShould
{
    private static readonly ListPolicy<string> Alphabetical = new(
        KeyOf: row => row,
        FilterFields: row => [row],
        Order: rows => rows.OrderBy(row => row, StringComparer.Ordinal),
        KeyComparison: StringComparison.Ordinal);

    private static readonly ListSelection<string> Loaded = Alphabetical.EmptyList().Load(["pear", "apple", "fig"]);

    [Fact]
    public void SelectTheFirstVisibleRowOnFirstLoad()
    {
        Loaded.Visible.ShouldBe(["apple", "fig", "pear"]);
        Loaded.Selected.ShouldBe("apple");
    }

    [Fact]
    public void KeepTheSelectedKeyAcrossAReload()
    {
        ListSelection<string> onFig = Loaded.Move(+1);

        onFig.Load(["fig", "kiwi", "apple"]).Selected.ShouldBe("fig");
    }

    [Fact]
    public void MoveToTheNearestRowWhenTheSelectionDisappears()
    {
        ListSelection<string> onPear = Loaded.Move(+2);

        onPear.Load(["apple", "fig"]).Selected.ShouldBe("fig");
        onPear.Load([]).Selected.ShouldBeNull();
    }

    [Fact]
    public void BeLoadedOnlyAfterTheFirstLoad()
    {
        Alphabetical.EmptyList().Loaded.ShouldBeFalse();
        Loaded.Loaded.ShouldBeTrue();
        Loaded.Load([]).Loaded.ShouldBeTrue();
    }

    [Fact]
    public void MoveWithinBounds()
    {
        Loaded.Move(-1).Selected.ShouldBe("apple");
        Loaded.Move(+1).Selected.ShouldBe("fig");
        Loaded.Move(+10).Selected.ShouldBe("pear");
    }

    [Fact]
    public void IgnoreMovesOnAnEmptyList()
    {
        ListSelection<string> empty = Alphabetical.EmptyList();

        empty.Move(+1).ShouldBe(empty);
    }

    [Fact]
    public void FilterCaseInsensitivelyAndReselect()
    {
        ListSelection<string> typing = Loaded.StartFilter().EditFilter(Key.Of('P'));

        typing.FilterEditing.ShouldBeTrue();
        typing.Visible.ShouldBe(["apple", "pear"]);
        typing.EditFilter(Key.Of('e')).Visible.ShouldBe(["pear"]);
        typing.EditFilter(Key.Of('e')).Selected.ShouldBe("pear");
    }

    [Fact]
    public void KeepTheFilterOnEnter()
    {
        ListSelection<string> committed = Loaded.StartFilter().EditFilter(Key.Of('f')).EditFilter(Key.Enter);

        committed.FilterEditing.ShouldBeFalse();
        committed.Visible.ShouldBe(["fig"]);
    }

    [Fact]
    public void ClearTheFilterOnEscape()
    {
        ListSelection<string> cleared = Loaded.StartFilter().EditFilter(Key.Of('f')).EditFilter(Key.Escape);

        cleared.FilterEditing.ShouldBeFalse();
        cleared.Filter.Text.ShouldBeEmpty();
        cleared.Visible.Count.ShouldBe(3);
        cleared.Selected.ShouldBe("fig");
    }

    [Fact]
    public void BackspaceShortensTheFilter()
    {
        ListSelection<string> typing = Loaded.StartFilter().EditFilter(Key.Of('f')).EditFilter(Key.Of('x'));

        typing.Visible.ShouldBeEmpty();
        typing.EditFilter(Key.Backspace).Visible.ShouldBe(["fig"]);
        typing.EditFilter(Key.Backspace).EditFilter(Key.Backspace).EditFilter(Key.Backspace).Filter.Text.ShouldBeEmpty();
    }

    [Fact]
    public void TitleNamesTheViewFilterAndCount()
    {
        Loaded.Title("fruit").ShouldBe("fruit(all)[3]");
        Loaded.StartFilter().EditFilter(Key.Of('p')).Title("fruit").ShouldBe("fruit(p)[2]");
    }

    [Fact]
    public void SelectAKeyAndClearAFilterThatHidesIt()
    {
        ListSelection<string> filtered = Loaded.StartFilter().EditFilter(Key.Of('f')).EditFilter(Key.Enter);

        ListSelection<string> selected = filtered.Select("pear");

        selected.Selected.ShouldBe("pear");
        selected.Filter.Text.ShouldBeEmpty();
        Loaded.Select("kiwi").Selected.ShouldBe("apple");
    }
}