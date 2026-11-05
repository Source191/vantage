using Microsoft.Extensions.Time.Testing;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;
using Source191.Torni.TestDoubles;

namespace Source191.Torni.Core.Tests.Application.Diagnostics;

public sealed class AttemptShould
{
    private readonly DiagnosticLog log = new(new FakeTimeProvider(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero)), []);

    [Fact]
    public async Task ReturnTheResultAndRecordNothing()
    {
        string result = await Attempt.RunAsync(log, FailureSite.Load, CodeName.For(typeof(SessionsViewState)), () => Task.FromResult("loaded"), exception => exception.Message);

        result.ShouldBe("loaded");
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerTheFailureAndRecordItOnce()
    {
        string result = await Attempt.RunAsync<string>(log, FailureSite.Load, CodeName.For(typeof(SessionsViewState)), () => throw new GitException(@"C:\Users\me\source\repos"), exception => $"message: {exception.Message}");

        result.ShouldBe(@"message: C:\Users\me\source\repos");
        Failed failed = log.Snapshot().Single().Event.ShouldBeOfType<Failed>();
        failed.Site.ShouldBe(FailureSite.Load);
        failed.What.ShouldBe(CodeName.For(typeof(SessionsViewState)));
        DiagnosticLine.Render(log.Snapshot().Single()).ShouldStartWith("2026-10-29T12:00:00.000Z failed Load SessionsViewState: GitException at AttemptShould.");
        DiagnosticLine.Render(log.Snapshot().Single()).ShouldNotContain(@"C:\Users\me");
    }

    [Fact]
    public async Task RecordOnlyTheFailuresTheFilterHandles()
    {
        string handled = await Attempt.RunAsync<string>(log, FailureSite.Load, CodeName.For(typeof(SessionsViewState)), () => throw new GitException("x"), exception => exception is GitException, _ => "blank");

        handled.ShouldBe("blank");
        log.Snapshot().Single().Event.ShouldBeOfType<Failed>();
    }

    [Fact]
    public async Task LetAFailureTheFilterRefusesPropagateUnrecorded()
    {
        Func<Task> cancelled = () => Attempt.RunAsync<string>(log, FailureSite.Load, CodeName.For(typeof(SessionsViewState)), () => throw new OperationCanceledException(), exception => exception is GitException, _ => "blank");

        await cancelled.ShouldThrowAsync<OperationCanceledException>();
        log.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task NameTheEffectAWorkingStatusMessageWaitsFor()
    {
        StatusMessage working = new StatusMessage.Working(new Quit());

        await Attempt.RunAsync<int>(log, FailureSite.Effect, Attempt.What(working), () => throw new InvalidOperationException(), _ => 0);

        log.Snapshot().Single().Event.ShouldBeOfType<Failed>().What.ShouldBe(CodeName.For(typeof(Quit)));
    }

    [Fact]
    public void NameAnyOtherStatusMessageByItsKind()
    {
        Attempt.What(new StatusMessage.WorkingSoRenameWaits(Listed.As("Fix flaky tests"))).ShouldBe(CodeName.For(typeof(StatusMessage.WorkingSoRenameWaits)));
    }
}