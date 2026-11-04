using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.TestDoubles;

public sealed class FakeWindowsTerminal : IWindowsTerminal
{
    private readonly List<TabLaunch> opened = [];
    private readonly List<(TerminalWindow Window, string Title)> focused = [];

    public IReadOnlyList<TabLaunch> Opened => opened;

    /// <summary>Every focus request, with the window it was aimed at.</summary>
    public IReadOnlyList<(TerminalWindow Window, string Title)> Focused => focused;

    /// <summary>Whether a focus request finds its tab; the real terminal answers false when no tab item matches.</summary>
    public bool FocusFinds { get; set; } = true;

    /// <summary>When set, opening a tab throws it, as Windows Terminal refusing the tab does; nothing is recorded.</summary>
    public TerminalException? FailTheOpenWith { get; set; }

    public Task OpenTabAsync(TabLaunch launch, CancellationToken cancellationToken)
    {
        if (FailTheOpenWith is not null)
        {
            throw FailTheOpenWith;
        }

        opened.Add(launch);
        return Task.CompletedTask;
    }

    public Task<bool> FocusTabAsync(TerminalWindow window, string title, CancellationToken cancellationToken)
    {
        focused.Add((window, title));
        return Task.FromResult(FocusFinds);
    }
}