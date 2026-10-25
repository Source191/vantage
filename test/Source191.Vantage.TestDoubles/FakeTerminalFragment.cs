using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>An in-memory fragment: writing sets it, removing clears it, and a failure can be queued for the next write.</summary>
public sealed class FakeTerminalFragment : ITerminalFragment
{
    private readonly List<TerminalProfiles> written = [];

    public string Location => @"C:\Users\me\AppData\Local\Microsoft\Windows Terminal\Fragments\Vantage\vantage.json";

    public bool Exists { get; private set; }

    /// <summary>Every write, oldest first.</summary>
    public IReadOnlyList<TerminalProfiles> Written => written;

    public int Removals { get; private set; }

    /// <summary>When set, the next write throws it.</summary>
    public IOException? FailTheWriteWith { get; set; }

    public FakeTerminalFragment Installed()
    {
        Exists = true;

        return this;
    }

    public Task WriteAsync(TerminalProfiles profiles, CancellationToken cancellationToken)
    {
        if (FailTheWriteWith is not null)
        {
            throw FailTheWriteWith;
        }

        written.Add(profiles);
        Exists = true;

        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(CancellationToken cancellationToken)
    {
        bool removed = Exists;
        Exists = false;
        Removals++;

        return Task.FromResult(removed);
    }
}