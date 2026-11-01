using Source191.Vantage.Application.Views;

namespace Source191.Vantage.Application.Detail;

/// <summary>Everything the pane shows for a selected session beyond the row itself.</summary>
public sealed record SessionDetail(SessionRow Row, LastPrompt LastPromptedAt, DetailFact<WorktreeCounts> Counts);