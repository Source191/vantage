namespace Source191.Vantage.Application.Views;

/// <summary>What a table screen needs to read from any view state: the list component plus the banner, the status message to show and the title.</summary>
public interface IListViewState<TRow>
{
    IReadOnlyList<TRow> Rows { get; }

    ListSelection<TRow> List { get; }

    LoadBanner Banner { get; }

    /// <summary>The status message to show: the last message, else the view's standing message.</summary>
    StatusMessage Shown { get; }

    bool Loaded { get; }

    TableTitle Title { get; }
}