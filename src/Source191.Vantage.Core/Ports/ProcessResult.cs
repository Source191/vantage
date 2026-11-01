namespace Source191.Vantage.Ports;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);