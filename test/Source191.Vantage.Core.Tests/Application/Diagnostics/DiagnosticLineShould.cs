using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class DiagnosticLineShould
{
    private static readonly DateTimeOffset At = new(2026, 10, 29, 14, 3, 12, 345, TimeSpan.Zero);

    [Fact]
    public void StartWithTheUtcTime()
    {
        DateTimeOffset local = new(2026, 10, 29, 15, 3, 12, 345, TimeSpan.FromHours(1));

        DiagnosticLine.Render(new TimedEvent(local, new ChildReturned())).ShouldBe("2026-10-29T14:03:12.345Z hand-off returned");
    }

    [Fact]
    public void RenderTheStart()
    {
        Started started = new(new ToolVersion("0.17.0-preview.4"), new Version(2, 1, 310), new Version(10, 0, 26100), new Version(10, 0, 1), 120, 36);

        Body(started).ShouldBe("started Vantage 0.17.0-preview.4, Claude Code 2.1.310, Windows 10.0.26100, .NET 10.0.1, terminal 120x36");
    }

    [Fact]
    public void RenderAStartWithoutAClaudeCodeVersion()
    {
        Started started = new(new ToolVersion("0.17.0"), null, new Version(10, 0, 26100), new Version(10, 0, 1), 120, 36);

        Body(started).ShouldBe("started Vantage 0.17.0, Claude Code unknown, Windows 10.0.26100, .NET 10.0.1, terminal 120x36");
    }

    [Fact]
    public void RenderAView()
    {
        Body(new ViewShown(ViewKind.Worktrees)).ShouldBe("view Worktrees");
    }

    [Fact]
    public void RenderADialog()
    {
        Body(new DialogShown(DialogName.Settings)).ShouldBe("dialog Settings");
    }

    [Fact]
    public void RenderAnEffectWithItsOutcomeAndDuration()
    {
        EffectRan ran = new(CodeName.For(typeof(StopSession)), CodeName.For(typeof(StopSessionOutcome)), TimeSpan.FromMilliseconds(412.7));

        Body(ran).ShouldBe("effect StopSession -> StopSessionOutcome in 412 ms");
    }

    [Fact]
    public void RenderAHandOff()
    {
        Body(new HandingOver(CodeName.For(typeof(AttachToSession)))).ShouldBe("hand-off AttachToSession");
    }

    [Fact]
    public void RenderAFailureWithWhatFailed()
    {
        Failed failed = new(FailureSite.Load, CodeName.For(typeof(SessionsViewState)), ExceptionSummary.From(new GitException("x")));

        Body(failed).ShouldBe("failed Load SessionsViewState: GitException");
    }

    [Fact]
    public void RenderAFailureWithoutWhatFailed()
    {
        Failed failed = new(FailureSite.Crash, null, ExceptionSummary.From(new GitException("x")));

        Body(failed).ShouldBe("failed Crash: GitException");
    }

    [Fact]
    public void RenderATerminalGuiReport()
    {
        Body(new TerminalGuiReported(DiagnosticLevel.Warning, null)).ShouldBe("terminal.gui Warning");
    }

    [Fact]
    public void RenderATerminalGuiReportWithItsException()
    {
        TerminalGuiReported reported = new(DiagnosticLevel.Error, ExceptionSummary.From(new GitException("x")));

        Body(reported).ShouldBe("terminal.gui Error: GitException");
    }

    private static string Body(DiagnosticEvent diagnostic)
    {
        return DiagnosticLine.Render(new TimedEvent(At, diagnostic))["2026-10-29T14:03:12.345Z ".Length..];
    }
}