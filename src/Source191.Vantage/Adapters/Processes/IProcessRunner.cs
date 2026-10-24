namespace Source191.Vantage.Adapters.Processes;

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments, string? WorkingDirectory);

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// The boundary to child processes. Captured runs return their output; interactive runs hand the
/// terminal to the child and return only its exit code.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessRequest request, CancellationToken cancellationToken);

    Task<int> RunInteractiveAsync(ProcessRequest request, CancellationToken cancellationToken);
}