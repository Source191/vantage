namespace Source191.Vantage.Application.Diagnostics;

public sealed record ProcessCount(ChildCommand Command, int ExitCode, DurationFigures Durations);
