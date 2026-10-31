using Source191.Vantage.Application.Launch;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>An effect that stops the screen: the exit, or the attach only <c>vantage here</c> runs, which <c>ScreenLoop</c> refuses from the console.</summary>
public interface ILeavesScreen;

/// <summary>An effect that shows the other tab of the console, or a row on it; the console applies it without leaving the screen.</summary>
public interface ISwitchesTab;

/// <summary>An effect whose flow includes a dialog, before it runs or about what it left behind; the answer comes back as an input or another effect.</summary>
public interface IDialogEffect;

/// <summary>Resume these jobs under the supervisor; raised once the checklist has been answered.</summary>
public sealed record ResumeSessions(IReadOnlyList<BackgroundSession> Sessions) : ViewEffect;

/// <summary>Delete the branch a removal left behind, after the user agreed.</summary>
public sealed record DeleteBranch(BranchToConfirm Branch) : ViewEffect;

/// <summary>Start the session the launch wizard planned, from either view, and open its tab when asked.</summary>
public sealed record StartSession(LaunchPlan Plan) : ViewEffect;