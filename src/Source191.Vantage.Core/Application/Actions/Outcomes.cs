using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Actions;

/// <summary>Which sessions a resume runs for at once (failed) and which it offers as a checklist (stopped).</summary>
public sealed record ResumePlan(IReadOnlyList<SessionRow> Automatic, IReadOnlyList<SessionRow> ToChoose);