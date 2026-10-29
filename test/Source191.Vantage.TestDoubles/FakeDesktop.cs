using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Records the files opened and the programs started instead of touching the desktop.</summary>
public sealed class FakeDesktop : IDesktop
{
    public List<string> Opened { get; } = [];

    public List<ProcessRequest> Started { get; } = [];

    /// <summary>When set, opening throws this, as Windows does when it cannot start the associated application.</summary>
    public Exception? OpenFailure { get; set; }

    public Task OpenAsync(string file, CancellationToken cancellationToken)
    {
        if (OpenFailure is not null)
        {
            throw OpenFailure;
        }

        Opened.Add(file);

        return Task.CompletedTask;
    }

    public Task StartAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        Started.Add(request);

        return Task.CompletedTask;
    }
}