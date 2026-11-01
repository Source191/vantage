using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Resume these jobs under the supervisor; raised by R directly for every shown stopped or failed session, with no checklist.</summary>
public sealed record ResumeSessions(IReadOnlyList<BackgroundSession> Sessions) : ViewEffect;