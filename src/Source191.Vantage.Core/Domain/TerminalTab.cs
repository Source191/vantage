namespace Source191.Vantage.Domain;

/// <summary>
/// Walks up from a process to the Windows Terminal that holds it: the shell is the process whose parent is the
/// WindowsTerminal process (any case), within eight steps. Ending that shell cleanly closes the tab. Null when the
/// process is not inside a tab. Windows never clears a parent id and reuses ids, so a parent that started after its
/// child, or whose start cannot be read, is not its real parent: the walk stops there and answers no tab, so an
/// unrelated tab's shell under a reused id is never ended.
/// </summary>
public static class TerminalTab
{
    private const int MaxDepth = 8;

    public static int? ShellOf(int processId, IProcessRegistry processes) => Walk(processId, processes)?.Shell;

    public static int? TerminalOf(int processId, IProcessRegistry processes) => Walk(processId, processes)?.Terminal;

    private static (int Shell, int Terminal)? Walk(int processId, IProcessRegistry processes)
    {
        int current = processId;
        DateTimeOffset? currentStarted = processes.StartTimeOf(processId);
        for (int depth = 0; depth < MaxDepth; depth++)
        {
            ProcessParent? parent = processes.ParentOf(current);
            DateTimeOffset? parentStarted = parent is null ? null : processes.StartTimeOf(parent.ProcessId);
            if (parent is null || !StartedNoLaterThan(parentStarted, currentStarted))
            {
                return null;
            }

            if (string.Equals(parent.Name, "WindowsTerminal", StringComparison.OrdinalIgnoreCase))
            {
                return (current, parent.ProcessId);
            }

            current = parent.ProcessId;
            currentStarted = parentStarted;
        }

        return null;
    }

    private static bool StartedNoLaterThan(DateTimeOffset? parent, DateTimeOffset? child) => parent is DateTimeOffset parentStart && child is DateTimeOffset childStart && parentStart <= childStart;
}