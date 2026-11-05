namespace Source191.Torni.Application.Diagnostics;

public sealed record ProcessCount(ChildCommand Command, int ExitCode, DurationFigures Durations);