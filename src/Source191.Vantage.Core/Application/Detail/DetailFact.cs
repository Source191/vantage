namespace Source191.Vantage.Application.Detail;

/// <summary>One fact of a row's detail, read on its own: known, not applicable to this row, or unavailable because reading it failed.</summary>
public abstract record DetailFact<T>
{
    public sealed record Known(T Value) : DetailFact<T>;

    /// <summary>The row has nothing of this kind, such as git counts for a folder outside a repository.</summary>
    public sealed record NotApplicable : DetailFact<T>;

    public sealed record Unavailable : DetailFact<T>;
}