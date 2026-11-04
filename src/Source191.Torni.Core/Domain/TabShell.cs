namespace Source191.Torni.Domain;

/// <summary>Where a process's Windows Terminal tab has its shell: above the process, the process itself, or nowhere.</summary>
public abstract record TabShell
{
    /// <summary>A shell sits between the process and Windows Terminal; ending it cleanly closes the tab.</summary>
    public sealed record Above(ConfirmedProcess Shell) : TabShell;

    /// <summary>Windows Terminal started the process directly, so ending it cleanly closes the tab.</summary>
    public sealed record TheProcess : TabShell;

    /// <summary>The process is not inside a tab Torni can trust.</summary>
    public sealed record None : TabShell;
}