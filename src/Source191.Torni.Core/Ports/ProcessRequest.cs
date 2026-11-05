namespace Source191.Torni.Ports;

public sealed record ProcessRequest(string FileName, IReadOnlyList<string> Arguments, WorkingFolder WorkingDirectory);