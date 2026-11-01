namespace Source191.Vantage.Application.Diagnostics;

public sealed record ProcessTimedOut(ChildCommand Command, TimeSpan Limit) : DiagnosticEvent;
