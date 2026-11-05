namespace Source191.Torni.Application.Views;

/// <summary>What a table screen needs to read from any view state: the list component plus the banner, the status message to show and the title.</summary>
public interface IListViewState<TRow>
{
    ListSelection<TRow> List { get; }

    LoadBanner Banner { get; }

    /// <summary>The last message, which is the status line; <see cref="StatusMessage.None"/> shows no status line.</summary>
    StatusMessage Message { get; }

    TableTitle Title { get; }
}