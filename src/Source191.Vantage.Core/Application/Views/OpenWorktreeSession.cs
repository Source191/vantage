using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Enter on a worktree: open its session's tab as the Sessions tab's Enter does; by id, because names are not unique, and the name for when it is not listed.</summary>
public sealed record OpenWorktreeSession(SessionId SessionId, string Name) : ViewEffect, IConsultsSessions;