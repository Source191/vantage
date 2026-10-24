using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

public sealed class FakeWindowsTerminal : IWindowsTerminal
{
    private readonly List<TabLaunch> opened = [];
    private readonly List<(int? TerminalProcessId, string Title)> focused = [];

    public IReadOnlyList<TabLaunch> Opened => opened;

    /// <summary>Every focus request, with the terminal process it was aimed at (null means any window).</summary>
    public IReadOnlyList<(int? TerminalProcessId, string Title)> Focused => focused;

    /// <summary>Whether a focus request finds its tab; the real terminal answers false when no tab item matches.</summary>
    public bool FocusFinds { get; set; } = true;

    public Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        opened.Add(launch);
        return Task.CompletedTask;
    }

    public Task<bool> FocusTabAsync(int? terminalProcessId, string title, CancellationToken cancellationToken)
    {
        focused.Add((terminalProcessId, title));
        return Task.FromResult(FocusFinds);
    }
}