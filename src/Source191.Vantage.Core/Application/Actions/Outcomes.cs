using Source191.Vantage.Application.Views;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>What deleting a session did, and the branch the user is asked about when the worktree's branch was not merged.</summary>
public sealed record DeleteOutcome(string Message, BranchToConfirm? BranchToConfirm);

/// <summary>Which sessions a resume runs for at once (failed) and which it offers as a checklist (stopped).</summary>
public sealed record ResumePlan(IReadOnlyList<SessionRow> Automatic, IReadOnlyList<SessionRow> ToChoose);