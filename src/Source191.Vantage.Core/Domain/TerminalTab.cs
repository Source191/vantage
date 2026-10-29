namespace Source191.Vantage.Domain;

/// <summary>
/// Walks up from a process to the Windows Terminal that holds it: the shell is the process whose parent is the
/// WindowsTerminal process (any case), within eight steps. Ending that shell cleanly closes the tab. Null when the
/// process is not inside a tab.
/// </summary>
public static class TerminalTab
{
    private const int MaxDepth = 8;

    public static int? ShellOf(int processId, IProcessRegistry processes) => Walk(processId, processes)?.Shell;

    public static int? TerminalOf(int processId, IProcessRegistry processes) => Walk(processId, processes)?.Terminal;

    private static (int Shell, int Terminal)? Walk(int processId, IProcessRegistry processes)
    {
        int current = processId;
        for (int depth = 0; depth < MaxDepth; depth++)
        {
            ProcessParent? parent = processes.ParentOf(current);
            if (parent is null)
            {
                return null;
            }

            if (string.Equals(parent.Name, "WindowsTerminal", StringComparison.OrdinalIgnoreCase))
            {
                return (current, parent.ProcessId);
            }

            current = parent.ProcessId;
        }

        return null;
    }
}