namespace Source191.Vantage.Application.Views;

/// <summary>What a table screen needs to read from any view state: the list component plus the banner, the footer to show and the title.</summary>
public interface IListViewState<TRow>
{
    IReadOnlyList<TRow> Rows { get; }

    ListSelection<TRow> List { get; }

    string? Banner { get; }

    /// <summary>The footer to show: the last message, else the view's standing footer.</summary>
    Footer Shown { get; }

    bool Loaded { get; }

    string Title { get; }
}