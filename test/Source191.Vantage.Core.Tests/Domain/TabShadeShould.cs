using Source191.Vantage.Domain;
using System.Globalization;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class TabShadeShould
{
    private static readonly int[] ChannelStarts = [1, 3, 5];

    /// <summary>Each palette colour's unselected tab as Windows Terminal 1.25.2733.0 drew it, measured on 30 October 2026.</summary>
    public static TheoryData<string, string> MeasuredShades => new()
    {
        { "Red", "#653539" },
        { "Blue", "#204460" },
        { "Yellow", "#6D5720" },
        { "Purple", "#483E57" },
        { "Green", "#265B23" },
        { "Orange", "#6A3D23" },
        { "Teal", "#20575A" },
        { "Pink", "#64204A" },
        { "Brown", "#4A392D" },
        { "Lime", "#4E5D2F" },
        { "Grey", "#444343" },
    };

    [Theory]
    [MemberData(nameof(MeasuredShades))]
    public void MatchTheMeasuredUnselectedTabWithinOneStepPerChannel(string name, string measured)
    {
        TabColour colour = TabPalette.Default.Single(candidate => candidate.Name == name);

        string shade = TabShade.Unselected(colour);

        int[] differences = [.. Channels(shade).Zip(Channels(measured), (ours, theirs) => Math.Abs(ours - theirs))];
        differences.ShouldAllBe(difference => difference <= 1);
    }

    [Fact]
    public void BlendTheColourAtThirtyPerCentOverTheTabRow()
    {
        TabShade.Unselected(new TabColour("Red", "#E74856")).ShouldBe("#65353A");
    }

    [Fact]
    public void LeaveTheTabRowWhenTheColourIsTheTabRow()
    {
        TabShade.Unselected(new TabColour("Row", "#2E2E2E")).ShouldBe("#2E2E2E");
    }

    [Fact]
    public void CoverAColourWrittenInLowerCase()
    {
        TabShade.Unselected(new TabColour("Red", "#e74856")).ShouldBe("#65353A");
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("E74856")]
    [InlineData("#E7485")]
    [InlineData("#FFE74856")]
    [InlineData("#GG4856")]
    public void LeaveAColourItCannotReadAsItIs(string hex)
    {
        TabShade.Unselected(new TabColour("Red", hex)).ShouldBe(hex);
    }

    private static int[] Channels(string hex)
    {
        return [.. ChannelStarts.Select(start => int.Parse(hex.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))];
    }
}