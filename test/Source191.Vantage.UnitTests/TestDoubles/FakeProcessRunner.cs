using Source191.Vantage.Adapters.Processes;

namespace Source191.Vantage.UnitTests.TestDoubles;

/// <summary>
/// Records every process request and answers captured runs with canned results, newest rule first. Each run yields
/// once, like a real process, so callers that overlap really do overlap; the lists are guarded for that reason.
/// </summary>
public sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Lock gate = new();
    private readonly List<Rule> rules = [];
    private readonly List<ProcessRequest> captured = [];
    private readonly List<ProcessRequest> interactive = [];

    public IReadOnlyList<ProcessRequest> CapturedRequests
    {
        get
        {
            lock (gate)
            {
                return [.. captured];
            }
        }
    }

    public IReadOnlyList<ProcessRequest> InteractiveRequests => interactive;

    public int InteractiveExitCode { get; set; }

    public FakeProcessRunner Answer(Func<ProcessRequest, bool> matches, ProcessResult result)
    {
        rules.Insert(0, new Rule(matches, new Queue<ProcessResult>([result])));
        return this;
    }

    public FakeProcessRunner AnswerAll(ProcessResult result)
    {
        return Answer(_ => true, result);
    }

    /// <summary>Answers matching requests with each result in turn, repeating the last one.</summary>
    public FakeProcessRunner AnswerInTurn(Func<ProcessRequest, bool> matches, params ProcessResult[] results)
    {
        rules.Insert(0, new Rule(matches, new Queue<ProcessResult>(results)));
        return this;
    }

    public async Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        lock (gate)
        {
            captured.Add(request);
            return rules.First(rule => rule.Matches(request)).Next();
        }
    }

    public Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        interactive.Add(request);
        return Task.FromResult(InteractiveExitCode);
    }

    private sealed class Rule(Func<ProcessRequest, bool> matches, Queue<ProcessResult> results)
    {
        public bool Matches(ProcessRequest request)
        {
            return matches(request);
        }

        public ProcessResult Next()
        {
            return results.Count > 1 ? results.Dequeue() : results.Peek();
        }
    }
}