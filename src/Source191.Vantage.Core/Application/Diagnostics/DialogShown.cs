namespace Source191.Vantage.Application.Diagnostics;

public sealed record DialogShown(DialogName Dialog) : DiagnosticEvent;