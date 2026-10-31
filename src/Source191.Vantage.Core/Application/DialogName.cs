namespace Source191.Vantage.Application;

/// <summary>Which dialog is open, for the word Vantage's tab shows beside its name while it is (<see cref="Wording.TabTitleDuring(DialogName)"/>).</summary>
public enum DialogName
{
    Keys,
    NewSession,
    Rename,
    Resume,
    Settings,
    FolderPicker,
    DeleteSession,
    DeleteBranch,
    CloseEmpty,
    ForceRemove,
    OpenSolution,
    Update,
    Memory,
    ProblemReport,
}