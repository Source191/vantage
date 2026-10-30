using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.TestDoubles;

public sealed class FakeProcessRunnerShould
{
    private static readonly ProcessRequest Listing = new("git", ["worktree", "list"], null);

    [Fact]
    public async Task AnswerInTurnThenRepeatTheLastResult()
    {
        FakeProcessRunner runner = new FakeProcessRunner()
            .AnswerAll(new ProcessResult(0, "default", string.Empty))
            .AnswerInTurn(request => request.Arguments.Contains("list"), new ProcessResult(0, "first", string.Empty), new ProcessResult(0, "second", string.Empty));

        string one = (await runner.RunAsync(Listing, CancellationToken.None)).StandardOutput;
        string two = (await runner.RunAsync(Listing, CancellationToken.None)).StandardOutput;
        string three = (await runner.RunAsync(Listing, CancellationToken.None)).StandardOutput;

        string[] answers = [one, two, three];
        answers.ShouldBe(["first", "second", "second"]);
    }

    [Fact]
    public async Task ThrowWhatItWasToldToFailWith()
    {
        FakeProcessRunner runner = new FakeProcessRunner().Fail(new InvalidOperationException("no such program"));

        InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(() => runner.RunAsync(Listing, CancellationToken.None));

        thrown.Message.ShouldBe("no such program");
        runner.CapturedRequests.Single().ShouldBe(Listing);
    }
}