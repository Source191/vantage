using Source191.Vantage.Application;
using Source191.Vantage.Application.Detail;

namespace Source191.Vantage.Core.Tests.Application.Detail;

public sealed class DetailLayoutShould
{
    private static readonly IReadOnlyList<IReadOnlyList<DetailLine>> Facts =
    [
        [new("State", "needs input", TextTone.Amber), new("Started", "14:02"), new("Prompted", "14:03")],
        [new("Folder", @"C:\f", Span: 2), new("Session", "s1")],
    ];

    [Fact]
    public void LineTheFactsUpInColumnsGivingTheLastTheSpareWidth()
    {
        DetailContent content = new("fix-123", Facts, Below: null, KeepsLastLines: true);

        IReadOnlyList<IReadOnlyList<TextPart>> rows = DetailLayout.Rows(content, width: 60, height: 5);

        Text(rows).ShouldBe(
        [
            "State  needs input  Started 14:02  Prompted 14:03           ",
            @"Folder C:\f                        Session  s1              ",
        ]);
    }

    [Fact]
    public void ColourLabelsOrangeAndTextInItsTone()
    {
        DetailContent content = new("fix-123", Facts, Below: null, KeepsLastLines: true);

        IReadOnlyList<TextPart> first = DetailLayout.Rows(content, width: 60, height: 5)[0];

        first[0].ShouldBe(new TextPart("State  ", TextTone.Orange));
        first[1].ShouldBe(new TextPart("needs input", TextTone.Amber));
    }

    [Fact]
    public void NarrowTheWidestColumnFirstAndCutWhatNoLongerFits()
    {
        DetailContent content = new("fix-123", Facts, Below: null, KeepsLastLines: true);

        IReadOnlyList<IReadOnlyList<TextPart>> rows = DetailLayout.Rows(content, width: 40, height: 5);

        Text(rows)[0].ShouldBe("State  need…  Started 14:…  Prompted 14…");
        Text(rows).ShouldAllBe(row => row.Length == 40);
    }

    [Fact]
    public void KeepTheEndOfAScreenThatDoesNotFit()
    {
        DetailContent content = new("fix-123", Facts, [new(string.Empty, "one"), new(string.Empty, "two"), new(string.Empty, "three")], KeepsLastLines: true);

        Text(DetailLayout.Rows(content, width: 60, height: 4))[2..].Select(row => row.TrimEnd()).ShouldBe(["two", "three"]);
    }

    [Fact]
    public void KeepTheStartOfAListingWithItsLabelsAligned()
    {
        DetailContent content = new("Northwind.Orders-fix-123", Facts, [new("Unpushed", "a1b2c31"), new(string.Empty, "a1b2c32"), new(string.Empty, "… 3 more", TextTone.Grey)], KeepsLastLines: false);

        Text(DetailLayout.Rows(content, width: 60, height: 4))[2..].Select(row => row.TrimEnd()).ShouldBe(["Unpushed a1b2c31", "         a1b2c32"]);
    }

    [Fact]
    public void ShowAsManyFactRowsAsThePaneHas()
    {
        DetailContent content = new("fix-123", Facts, [new(string.Empty, "one")], KeepsLastLines: true);

        DetailLayout.Rows(content, width: 60, height: 1).Count.ShouldBe(1);
        DetailLayout.Rows(content, width: 60, height: 0).ShouldBeEmpty();
    }

    private static string[] Text(IReadOnlyList<IReadOnlyList<TextPart>> rows) => [.. rows.Select(row => string.Concat(row.Select(part => part.Text)))];
}
