namespace Source191.Vantage.Domain;

public sealed class Sessions(IReadOnlyList<Session> items)
{
    public IReadOnlyList<Session> OrderedByAttention()
    {
        return [.. items
            .OrderBy(session => AttentionRank(session.State.Kind))
            .ThenByDescending(session => session.StartedAt)];
    }

    private static int AttentionRank(DisplayStateKind kind)
    {
        return kind switch
        {
            DisplayStateKind.NeedsInput => 0,
            DisplayStateKind.Working => 1,
            DisplayStateKind.Idle => 2,
            DisplayStateKind.Completed => 3,
            DisplayStateKind.Failed => 4,
            DisplayStateKind.Stopped => 5,
            DisplayStateKind.Unknown => 6,
        };
    }
}