namespace Source191.Torni.Application.Diagnostics;

public sealed record ProcessFailedToStart(ChildCommand Command, ExceptionSummary Failure) : DiagnosticEvent;