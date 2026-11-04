namespace Source191.Torni.Ports;

/// <summary>
/// Where a program Torni runs was found: the full path of its executable, or nowhere. Each adapter that needs one
/// says what a missing program means where it is needed (a gap, a refused open, an exception's sentence).
/// </summary>
public abstract record ProgramLocation
{
    public sealed record Found(string Path) : ProgramLocation;

    public sealed record Missing : ProgramLocation;
}