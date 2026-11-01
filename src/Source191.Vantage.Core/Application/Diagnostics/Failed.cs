namespace Source191.Vantage.Application.Diagnostics;

/// <summary>A failure caught where it was turned into a status message, a load failure or a crash; <paramref name="What"/> is what was running, when there was one thing.</summary>
public sealed record Failed(FailureSite Site, CodeName? What, ExceptionSummary Failure) : DiagnosticEvent;