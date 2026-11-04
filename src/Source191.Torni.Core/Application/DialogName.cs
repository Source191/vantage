namespace Source191.Torni.Application;

/// <summary>Which dialog is open, for the word Torni's tab shows beside its name while it is (<see cref="Wording.TabTitleDuring(DialogName)"/>).</summary>
public enum DialogName
{
    Keys,
    NewSession,
    Rename,
    Settings,
    FolderPicker,
    FilePicker,
    DeleteSession,
    DeleteBranch,
    CloseEmpty,
    ForceRemove,
    OpenSolution,
    WhatsNew,
    Memory,
    ProblemReport,
}