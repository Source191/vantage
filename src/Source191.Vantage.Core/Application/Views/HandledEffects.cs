using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>An effect that stops the screen: it needs the terminal (attach, the agent view), another screen, or the exit.</summary>
public interface ILeavesScreen;

/// <summary>An effect that opens a dialog before anything runs; the dialog's answer comes back as an input or another effect.</summary>
public interface IDialogEffect;

/// <summary>Resume these sessions under the supervisor; raised once the checklist has been answered.</summary>
public sealed record ResumeSessions(IReadOnlyList<SessionRow> Rows) : ViewEffect;

/// <summary>Delete the branch a removal left behind, after the user agreed.</summary>
public sealed record DeleteBranch(BranchToConfirm Branch) : ViewEffect;