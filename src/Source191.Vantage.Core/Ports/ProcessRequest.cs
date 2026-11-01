namespace Source191.Vantage.Ports;

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments, WorkingFolder WorkingDirectory);