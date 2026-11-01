namespace Source191.Vantage.Ports;

/// <summary>
/// Windows refused to start a program <see cref="IDesktop.StartAsync"/> was given (access denied, not a program, a
/// broken install). <see cref="Reason"/> is Windows' own sentence for its error, without the path.
/// </summary>
public sealed class ProgramNotStartedException(string reason) : Exception(reason)
{
    public string Reason { get; } = reason;
}
