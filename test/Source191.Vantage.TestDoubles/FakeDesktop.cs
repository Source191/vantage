using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Records the files opened and the programs started instead of touching the desktop.</summary>
public sealed class FakeDesktop : IDesktop
{
    public List<string> Opened { get; } = [];

    public List<ProcessRequest> Started { get; } = [];

    public Task OpenAsync(string file, CancellationToken cancellationToken)
    {
        Opened.Add(file);

        return Task.CompletedTask;
    }

    public Task StartAsync(ProcessRequest request, CancellationToken cancellationToken)
    {
        Started.Add(request);

        return Task.CompletedTask;
    }
}