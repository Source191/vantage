using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Records each copy asked for, runs a callback when one arrives and can be made to fail, in place of the real file copy.</summary>
public sealed class FakeWorktreeIncludeCopier : IWorktreeIncludeCopier
{
    private readonly List<(string Repository, string Worktree)> calls = [];
    private Exception? failure;
    private Action onCopy = static () => { };

    public IReadOnlyList<(string Repository, string Worktree)> Calls => calls;

    /// <summary>Makes every copy throw the given exception, as a file the file system refuses would.</summary>
    public FakeWorktreeIncludeCopier Throwing(Exception exception)
    {
        failure = exception;

        return this;
    }

    /// <summary>Runs the action as each copy starts, before it fails or answers.</summary>
    public FakeWorktreeIncludeCopier WhenCopying(Action action)
    {
        onCopy = action;

        return this;
    }

    public Task<IReadOnlyList<string>> CopyAsync(string repository, string worktree, CancellationToken cancellationToken)
    {
        calls.Add((repository, worktree));
        onCopy();

        return failure is null ? Task.FromResult<IReadOnlyList<string>>([]) : Task.FromException<IReadOnlyList<string>>(failure);
    }
}