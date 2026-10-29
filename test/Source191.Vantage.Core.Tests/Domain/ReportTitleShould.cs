using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

public sealed class ReportTitleShould
{
    [Fact]
    public void AcceptATypedTitle()
    {
        ReportTitle.Validate("Attach froze after Ctrl+Z").ShouldBe(new ReportTitleValidation.Valid(ReportTitle.From("Attach froze after Ctrl+Z")));
    }

    [Fact]
    public void AcceptTwoHundredCharacters()
    {
        ReportTitle.Validate(new string('a', 200)).ShouldBeOfType<ReportTitleValidation.Valid>();
    }

    [Fact]
    public void RefuseMoreThanTwoHundredCharacters()
    {
        ReportTitle.Validate(new string('a', 201)).ShouldBe(new ReportTitleValidation.Refused(ReportTitleProblem.TooLong));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RefuseABlankTitle(string text)
    {
        ReportTitle.Validate(text).ShouldBe(new ReportTitleValidation.Refused(ReportTitleProblem.Missing));
    }

    [Fact]
    public void ThrowFromForTextThatBreaksTheRules()
    {
        Should.Throw<ArgumentException>(() => ReportTitle.From(" "));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("Attach froze", true)]
    public void ParseOnlyAGoodTitle(string? text, bool parses)
    {
        ReportTitle.TryParse(text, out ReportTitle? title).ShouldBe(parses);
        (title is not null).ShouldBe(parses);
    }
}