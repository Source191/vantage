namespace Source191.Vantage.Domain;

/// <summary>
/// Finds the shell that owns a Windows Terminal tab: walking up from a process, it is the one whose parent is the
/// WindowsTerminal process. Ending that shell cleanly closes the tab. Null when the process is not inside a tab.
/// </summary>
public static class TerminalTab
{
    private const int MaxDepth = 8;

    public static int? ShellOf(int processId, IProcessRegistry processes)
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
                return current;
            }

            current = parent.ProcessId;
        }

        return null;
    }
}