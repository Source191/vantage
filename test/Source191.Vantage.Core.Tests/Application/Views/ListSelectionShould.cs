using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ListSelectionShould
{
    private static readonly ListPolicy<string> Alphabetical = new(
        KeyOf: row => row,
        FilterFields: row => [row],
        Order: rows => rows.OrderBy(row => row, StringComparer.Ordinal),
        KeyComparison: StringComparison.Ordinal,
        Selectable: _ => true);

    /// <summary>Rows kept in the order given, where a row starting with # is a heading the selection steps over and any filter hides.</summary>
    private static readonly ListPolicy<string> Headed = new(
        KeyOf: row => row,
        FilterFields: row => row.StartsWith('#') ? [] : [row],
        Order: rows => rows,
        KeyComparison: StringComparison.Ordinal,
        Selectable: row => !row.StartsWith('#'));

    private static readonly ListSelection<string> HeadedLoaded = Headed.EmptyList().Load(["#fruit", "apple", "fig", "#veg", "kale"]);

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
    public void AnswerItsKeyAsASelectionKey()
    {
        Loaded.Selection.ShouldBe(new SelectionKey.Row("apple"));
        Loaded.Load([]).Selection.ShouldBe(new SelectionKey.None());
    }

    [Fact]
    public void BuildASelectionFromASelectionKey()
    {
        Alphabetical.List(["pear", "fig"], new SelectionKey.Row("pear"), RowFilter.None, filterEditing: false, loaded: true).Selected.ShouldBe("pear");
        Alphabetical.List(["pear", "fig"], new SelectionKey.None(), RowFilter.None, filterEditing: false, loaded: true).SelectedKey.ShouldBeNull();
    }

    [Fact]
    public void BeLoadedOnlyAfterTheFirstLoad()
    {
        Alphabetical.EmptyList().Loaded.ShouldBeFalse();
        Loaded.Loaded.ShouldBeTrue();
        Loaded.Load([]).Loaded.ShouldBeTrue();
    }

    [Fact]
    public void KeepUnloadedWhenAFilterIsEditedBeforeTheFirstLoad()
    {
        ListSelection<string> typing = Alphabetical.EmptyList().StartFilter();

        typing.EditFilter(Key.Of('a')).Loaded.ShouldBeFalse();
        typing.EditFilter(Key.Backspace).Loaded.ShouldBeFalse();
        typing.ClearFilter().Loaded.ShouldBeFalse();
        Loaded.StartFilter().EditFilter(Key.Of('p')).Loaded.ShouldBeTrue();
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
    public void CountShownAndTotalRowsAndLabelTheSelection()
    {
        Loaded.Title("fruit", row => row.ToUpperInvariant()).ShouldBe(new TableTitle("fruit", Shown: 3, Total: 3, Filter: string.Empty, Selected: "APPLE"));
        Loaded.StartFilter().EditFilter(Key.Of('p')).Title("fruit", row => row).ShouldBe(new TableTitle("fruit", Shown: 2, Total: 3, Filter: "p", Selected: "apple"));
    }

    [Fact]
    public void LeaveTheSelectionOutWhenNothingIsSelected()
    {
        Alphabetical.EmptyList().Load([]).Title("fruit", row => row).ShouldBe(new TableTitle("fruit", Shown: 0, Total: 0, Filter: string.Empty, Selected: null));
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

    [Fact]
    public void NeverSelectAnUnselectableRowOnFirstLoad()
    {
        HeadedLoaded.Selected.ShouldBe("apple");
    }

    [Fact]
    public void StepOverAnUnselectableRowWhenMoving()
    {
        ListSelection<string> onFig = HeadedLoaded.Move(+1);

        onFig.Selected.ShouldBe("fig");
        onFig.Move(+1).Selected.ShouldBe("kale");
        onFig.Move(+1).Move(-1).Selected.ShouldBe("fig");
        HeadedLoaded.Move(+2).Selected.ShouldBe("kale");
    }

    [Fact]
    public void StayPutAtEitherEndWhenTheRowBeyondIsUnselectable()
    {
        HeadedLoaded.Move(-1).Selected.ShouldBe("apple");
        HeadedLoaded.Move(+10).Selected.ShouldBe("kale");
        HeadedLoaded.Move(+10).Move(+1).Selected.ShouldBe("kale");
    }

    [Fact]
    public void PickTheNearestSelectableRowWhenTheSelectionDisappears()
    {
        ListSelection<string> onKale = HeadedLoaded.Move(+2);

        onKale.Load(["#fruit", "apple", "fig", "#veg"]).Selected.ShouldBe("fig");
    }

    [Fact]
    public void NeverLandOnAnUnselectableRowWhenAFilterIsCleared()
    {
        ListSelection<string> hidingAll = HeadedLoaded.StartFilter().EditFilter(Key.Of('z'));

        hidingAll.Selected.ShouldBeNull();
        hidingAll.EditFilter(Key.Escape).Selected.ShouldBe("apple");
    }

    [Fact]
    public void IgnoreSelectingAnUnselectableRow()
    {
        HeadedLoaded.Select("#veg").ShouldBe(HeadedLoaded);
    }

    [Fact]
    public void CountOnlySelectableRowsInTheTitle()
    {
        HeadedLoaded.Title("item", row => row).ShouldBe(new TableTitle("item", Shown: 3, Total: 3, Filter: string.Empty, Selected: "apple"));
        HeadedLoaded.StartFilter().EditFilter(Key.Of('a')).Title("item", row => row).ShouldBe(new TableTitle("item", Shown: 2, Total: 3, Filter: "a", Selected: "apple"));
    }
}