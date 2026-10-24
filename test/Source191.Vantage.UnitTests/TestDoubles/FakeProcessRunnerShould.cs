using Source191.Vantage.Adapters.Processes;

namespace Source191.Vantage.UnitTests.TestDoubles;

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

        new[] { one, two, three }.ShouldBe(["first", "second", "second"]);
    }
}