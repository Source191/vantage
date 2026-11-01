using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Resume these jobs under the supervisor; raised once the checklist has been answered.</summary>
public sealed record ResumeSessions(IReadOnlyList<BackgroundSession> Sessions) : ViewEffect;