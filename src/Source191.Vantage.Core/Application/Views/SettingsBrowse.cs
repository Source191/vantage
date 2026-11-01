namespace Source191.Vantage.Application.Views;

/// <summary>What a setting's dialog can browse for: nothing, the one worktree root, or several repository roots.</summary>
public enum SettingsBrowse
{
    No,
    OneFolder,
    SeveralFolders,
}