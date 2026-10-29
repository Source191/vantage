using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Application.Views;

public sealed class ProblemReportViewShould
{
    private static readonly ReportTitle Default = ReportTitle.From("Problem report from Vantage 0.18.0");
    private static readonly ProblemReportState Opened = ProblemReportState.Open(Default);

    [Fact]
    public void OpenWithAnEmptyTitleAndNoProblem()
    {
        Opened.ShouldBe(new ProblemReportState(string.Empty, Problem: null, Default));
    }

    [Fact]
    public void FollowTheTitleAsItIsTyped()
    {
        ProblemReportUpdate update = ProblemReportView.Reduce(Opened, new ProblemReportInput.TitleChanged("Attach froze"));

        update.State.TitleText.ShouldBe("Attach froze");
        update.Effect.ShouldBe(new ProblemReportEffect.None());
    }

    [Fact]
    public void SayATitleIsTooLongWhileItIsTyped()
    {
        ProblemReportUpdate update = ProblemReportView.Reduce(Opened, new ProblemReportInput.TitleChanged(new string('a', 201)));

        update.State.Problem.ShouldBe(ReportTitleProblem.TooLong);
    }

    [Fact]
    public void ClearTheProblemOnceTheTitleIsShortAgain()
    {
        ProblemReportState tooLong = ProblemReportView.Reduce(Opened, new ProblemReportInput.TitleChanged(new string('a', 201))).State;

        ProblemReportView.Reduce(tooLong, new ProblemReportInput.TitleChanged("short")).State.Problem.ShouldBeNull();
    }

    [Fact]
    public void OpenWithTheTypedTitle()
    {
        ProblemReportState typed = ProblemReportView.Reduce(Opened, new ProblemReportInput.TitleChanged("Attach froze")).State;

        ProblemReportView.Reduce(typed, new ProblemReportInput.Open()).Effect.ShouldBe(new ProblemReportEffect.Open(ReportTitle.From("Attach froze")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OpenWithTheDefaultTitleWhenTheBoxIsBlank(string text)
    {
        ProblemReportState blank = ProblemReportView.Reduce(Opened, new ProblemReportInput.TitleChanged(text)).State;

        ProblemReportView.Reduce(blank, new ProblemReportInput.Open()).Effect.ShouldBe(new ProblemReportEffect.Open(Default));
    }

    [Fact]
    public void RefuseToOpenOrCopyATitleThatIsTooLong()
    {
        ProblemReportState tooLong = ProblemReportView.Reduce(Opened, new ProblemReportInput.TitleChanged(new string('a', 201))).State;

        ProblemReportView.Reduce(tooLong, new ProblemReportInput.Open()).Effect.ShouldBe(new ProblemReportEffect.None());
        ProblemReportView.Reduce(tooLong, new ProblemReportInput.Copy()).Effect.ShouldBe(new ProblemReportEffect.None());
    }

    [Fact]
    public void CopyWithTheTitle()
    {
        ProblemReportView.Reduce(Opened, new ProblemReportInput.Copy()).Effect.ShouldBe(new ProblemReportEffect.Copy(Default));
    }

    [Fact]
    public void CloseOnCancel()
    {
        ProblemReportView.Reduce(Opened, new ProblemReportInput.Cancel()).Effect.ShouldBe(new ProblemReportEffect.Close());
    }
}
