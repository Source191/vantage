namespace Source191.Torni.Ports;

/// <summary>
/// Windows refused to start a program <see cref="IDesktop.StartAsync"/> was given (access denied, not a program, a
/// broken install), or <see cref="IDesktop.OpenAsync"/> could not start what opens a file or address.
/// <see cref="Reason"/> is Windows' own sentence for its error, without the path.
/// </summary>
public sealed class ProgramNotStartedException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}