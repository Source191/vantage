namespace Source191.Vantage.Application.Detail;

/// <summary>What the pane does with a read: show it, or drop it because the read was cancelled or failed, its row is no longer selected or a newer answer was shown.</summary>
public abstract record DetailAnswer
{
    public sealed record Show(DetailContent Content) : DetailAnswer;

    public sealed record Dropped : DetailAnswer;
}