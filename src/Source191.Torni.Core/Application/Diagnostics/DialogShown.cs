namespace Source191.Torni.Application.Diagnostics;

public sealed record DialogShown(DialogName Dialog) : DiagnosticEvent;