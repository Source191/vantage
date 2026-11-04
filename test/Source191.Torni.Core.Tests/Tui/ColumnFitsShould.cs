using Source191.Torni.Tui;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Source191.Torni.Core.Tests.Tui;

public sealed class ColumnFitsShould
{
    [Fact]
    public void CapAColumnAtItsCellsWithAnEllipsis()
    {
        ColumnStyle style = new();

        ColumnFits.Apply(style, new ColumnFit.Capped(28));

        style.MaxWidth.ShouldBe(28);
        style.TruncationIndicator.ShouldBe("…");
    }

    [Fact]
    public void RightAlignAColumn()
    {
        ColumnStyle style = new();

        ColumnFits.Apply(style, new ColumnFit.RightAligned());

        style.Alignment.ShouldBe(Alignment.End);
    }

    [Fact]
    public void LeaveNaturalAndElasticUnstyled()
    {
        ColumnStyle natural = new();
        ColumnStyle elastic = new();

        ColumnFits.Apply(natural, new ColumnFit.Natural());
        ColumnFits.Apply(elastic, new ColumnFit.Elastic());

        natural.Alignment.ShouldBe(new ColumnStyle().Alignment);
        elastic.MaxWidth.ShouldBe(new ColumnStyle().MaxWidth);
    }
}