using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Where a setting's folder picker opens: at a folder worked out from what is typed, or, with no full path typed, where Terminal.Gui chooses.</summary>
public abstract record BrowseStart
{
    public sealed record NoFolderTyped : BrowseStart;

    public sealed record At(FolderPath Folder) : BrowseStart;
}