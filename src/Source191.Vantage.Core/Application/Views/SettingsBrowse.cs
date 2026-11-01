namespace Source191.Vantage.Application.Views;

/// <summary>What a setting's dialog can browse for: nothing, the one worktree root, several repository roots, or a custom editor's executable.</summary>
public enum SettingsBrowse
{
    No,
    OneFolder,
    SeveralFolders,
    OneFile,
}