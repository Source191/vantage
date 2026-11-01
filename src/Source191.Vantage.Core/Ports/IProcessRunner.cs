namespace Source191.Vantage.Ports;

/// <summary>
/// The boundary to child processes. Captured runs return their output; interactive runs hand the
/// terminal to the child and return only its exit code.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);

    Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken);
}