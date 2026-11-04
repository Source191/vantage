namespace Source191.Torni.Domain;

/// <summary>
/// Walks up from a process to the Windows Terminal that holds it: the shell is the process whose parent is the
/// WindowsTerminal process (any case), within eight steps. Ending that shell cleanly closes the tab.
/// A process not inside a tab answers <see cref="TabShell.None"/>. Windows never clears a parent id and reuses ids, so a parent that started after its
/// child, or whose start cannot be read, is not its real parent: the walk stops there and answers no tab, so an
/// unrelated tab's shell under a reused id is never ended.
/// </summary>
public static class TerminalTab
{
    private const int MaxDepth = 8;

    public static TabShell ShellAbove(ConfirmedProcess process, IProcessRegistry processes) => Walk(process, processes) switch
    {
        (ConfirmedProcess shell, _) when shell.Id == process.Id => new TabShell.TheProcess(),
        (ConfirmedProcess shell, _) => new TabShell.Above(shell),
        null => new TabShell.None(),
    };

    public static int? TerminalOf(ConfirmedProcess process, IProcessRegistry processes) => Walk(process, processes)?.Terminal;

    private static (ConfirmedProcess Shell, int Terminal)? Walk(ConfirmedProcess process, IProcessRegistry processes)
    {
        ConfirmedProcess current = process;
        for (int depth = 0; depth < MaxDepth; depth++)
        {
            ProcessParent? parent = processes.ParentOf(current.Id);
            if (parent is null || !ConfirmedProcess.TryConfirmParent(parent.ProcessId, processes.StartTimeOf(parent.ProcessId), current, out ConfirmedProcess? confirmed))
            {
                return null;
            }

            if (string.Equals(parent.Name, "WindowsTerminal", StringComparison.OrdinalIgnoreCase))
            {
                return (current, parent.ProcessId);
            }

            current = confirmed;
        }

        return null;
    }
}