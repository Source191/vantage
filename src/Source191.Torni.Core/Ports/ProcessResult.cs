namespace Source191.Torni.Ports;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);