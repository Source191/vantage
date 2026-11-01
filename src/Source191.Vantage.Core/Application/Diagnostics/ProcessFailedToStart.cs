namespace Source191.Vantage.Application.Diagnostics;

public sealed record ProcessFailedToStart(ChildCommand Command, ExceptionSummary Failure) : DiagnosticEvent;