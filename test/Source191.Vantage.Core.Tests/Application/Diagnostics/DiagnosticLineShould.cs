using Source191.Vantage.Application;
using Source191.Vantage.Application.Actions;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Core.Tests.Architecture;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Core.Tests.Application.Diagnostics;

public sealed class DiagnosticLineShould
{
    private static readonly DateTimeOffset At = new(2026, 10, 29, 14, 3, 12, 345, TimeSpan.Zero);

    [Fact]
    public void StartWithTheUtcTime()
    {
        DateTimeOffset local = new(2026, 10, 29, 15, 3, 12, 345, TimeSpan.FromHours(1));

        DiagnosticLine.Render(new TimedEvent(local, new ViewShown(ViewKind.Sessions))).ShouldBe("2026-10-29T14:03:12.345Z view Sessions");
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
    public void RenderPackagedNotesThatCouldNotBeRead()
    {
        Body(new PackagedNotesUnreadable(new ToolVersion("0.21.0"))).ShouldBe("no readable release notes in the package of 0.21.0");
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

    [Theory]
    [InlineData(RefreshPart.SessionsListing, "slow refresh sessions listing in 3400 ms")]
    [InlineData(RefreshPart.SessionsEnrichment, "slow refresh sessions enrichment in 3400 ms")]
    [InlineData(RefreshPart.WorktreesScan, "slow refresh worktrees scan in 3400 ms")]
    [InlineData(RefreshPart.PullRequestRefresh, "slow refresh pull-request refresh in 3400 ms")]
    public void RenderASlowRefreshByPart(RefreshPart part, string line)
    {
        Body(new RefreshSlow(part, TimeSpan.FromMilliseconds(3400.9))).ShouldBe(line);
    }

    [Fact]
    public void RenderARunThatTimedOut()
    {
        ChildCommand agents = ChildCommand.For(new ProcessRequest(@"C:\Users\me\.local\bin\claude.exe", ["agents", "--json"], WorkingFolder.Inherit));

        Body(new ProcessTimedOut(agents, TimeSpan.FromSeconds(60))).ShouldBe("timed out claude agents --json after 60000 ms");
    }

    [Fact]
    public void RenderAStall()
    {
        Body(new UiThreadStalled(TimeSpan.FromMilliseconds(820))).ShouldBe("stalled ui thread for 820 ms");
    }

    [Fact]
    public void RenderACountWithItsWriteTimeAndTypicalFigures()
    {
        ProcessCounts counts = new();
        ChildCommand agents = ChildCommand.For(new ProcessRequest(@"C:\Users\me\.local\bin\claude.exe", ["agents", "--json"], WorkingFolder.Inherit));
        foreach (int milliseconds in Enumerable.Repeat(1650, 95).Concat(Enumerable.Repeat(2100, 24)))
        {
            counts.Add(agents, 0, TimeSpan.FromMilliseconds(milliseconds));
        }

        counts.Add(agents, 0, TimeSpan.FromMilliseconds(4900));

        DiagnosticLine.Render(counts, At).ShouldBe(["2026-10-29T14:03:12.345Z counts claude agents --json exit 0: 120 runs, median 1650 ms, p95 2100 ms, longest 4900 ms"]);
    }

    [Fact]
    public void RenderTheStallCountsEvenWithNone()
    {
        StallCounts stalls = new();
        PerformanceCounts counts = new(new ProcessCounts(), new RefreshCounts(TimeProvider.System, new DiagnosticLog(TimeProvider.System, [])), stalls);
        string[] none = [.. DiagnosticLine.Render(counts, At)];

        foreach (int milliseconds in (int[])[150, 180, 820])
        {
            stalls.Add(TimeSpan.FromMilliseconds(milliseconds));
        }

        none.ShouldBe(["2026-10-29T14:03:12.345Z stalls ui thread: 0 stalls"]);
        DiagnosticLine.Render(counts, At).ShouldBe(["2026-10-29T14:03:12.345Z stalls ui thread: 3 stalls, median 180 ms, p95 820 ms, longest 820 ms"]);
    }

    public static TheoryData<Type> EventTypes => DiagnosticEventsShould.EventTypes;

    [Theory]
    [MemberData(nameof(EventTypes))]
    public void RenderEveryEvent(Type type)
    {
        System.Reflection.ConstructorInfo constructor = type.GetConstructors().Single(candidate => candidate.GetParameters().All(parameter => parameter.ParameterType != type));
        object?[] arguments = [.. constructor.GetParameters().Select(parameter => Placeholder(parameter.ParameterType))];
        DiagnosticEvent diagnostic = (DiagnosticEvent)constructor.Invoke(arguments);

        Body(diagnostic).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RefuseAnEventItDoesNotKnow()
    {
        Should.Throw<UnreachableException>(() => DiagnosticLine.Render(new TimedEvent(At, new Unknown())));
    }

    private static object? Placeholder(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (type != underlying)
        {
            return null;
        }

        return underlying switch
        {
            _ when underlying.IsEnum => Enum.GetValues(underlying).GetValue(0),
            _ when underlying == typeof(int) => 2,
            _ when underlying == typeof(long) => 2L,
            _ when underlying == typeof(bool) => true,
            _ when underlying == typeof(TimeSpan) => TimeSpan.FromMilliseconds(412),
            _ when underlying == typeof(Version) => new Version(2, 1, 310),
            _ when underlying == typeof(ToolVersion) => new ToolVersion("0.20.0"),
            _ when underlying == typeof(CodeName) => CodeName.For(typeof(StopSession)),
            _ when underlying == typeof(ExceptionSummary) => ExceptionSummary.From(new GitException("x")),
            _ when underlying == typeof(ChildCommand) => ChildCommand.For(new ProcessRequest("git", ["status"], WorkingFolder.Inherit)),
            _ => throw new NotSupportedException($"{type.Name} needs a placeholder."),
        };
    }

    private sealed record Unknown : DiagnosticEvent;

    private static string Body(DiagnosticEvent diagnostic)
    {
        return DiagnosticLine.Render(new TimedEvent(At, diagnostic))["2026-10-29T14:03:12.345Z ".Length..];
    }
}