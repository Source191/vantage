using Source191.Vantage.Adapters.Terminal;

namespace Source191.Vantage.UnitTests.TestDoubles;

public sealed class FakeWindowsTerminal : IWindowsTerminal
{
    private readonly List<TabLaunch> opened = [];

    public IReadOnlyList<TabLaunch> Opened => opened;

    public Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        opened.Add(launch);
        return Task.CompletedTask;
    }
}