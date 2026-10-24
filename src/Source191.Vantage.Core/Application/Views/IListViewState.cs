namespace Source191.Vantage.Application.Views;

/// <summary>What a table screen needs to read from any view state: the list component plus the banner, footer and title.</summary>
public interface IListViewState<TRow>
{
    IReadOnlyList<TRow> Rows { get; }

    ListSelection<TRow> List { get; }

    string? Banner { get; }

    string Footer { get; }

    bool Loaded { get; }

    string Title { get; }
}