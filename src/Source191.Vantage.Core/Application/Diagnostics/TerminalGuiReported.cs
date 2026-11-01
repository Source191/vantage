namespace Source191.Vantage.Application.Diagnostics;

/// <summary>A Terminal.Gui warning or error, with none of Terminal.Gui's words: its messages can quote input it failed to parse.</summary>
public sealed record TerminalGuiReported(DiagnosticLevel Level, ExceptionSummary? Failure) : DiagnosticEvent;