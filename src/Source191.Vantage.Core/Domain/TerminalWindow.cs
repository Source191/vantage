namespace Source191.Vantage.Domain;

/// <summary>
/// Which Windows Terminal window holds a tab: the one whose process owns the tab's shell, or any window, while the
/// tab is still being claimed or when its shell's parent chain does not reach Windows Terminal.
/// </summary>
public abstract record TerminalWindow
{
    public sealed record Owning(int ProcessId) : TerminalWindow;

    public sealed record Any : TerminalWindow;
}