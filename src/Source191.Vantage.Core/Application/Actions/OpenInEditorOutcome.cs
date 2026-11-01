using Source191.Vantage.Application.Effects;
using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Actions;

/// <summary>What the editor key did.</summary>
public abstract record OpenInEditorOutcome : Outcome
{
    /// <summary>The editor was started on the folder.</summary>
    public sealed record Opened(string FolderName, EditorChoice Editor) : OpenInEditorOutcome;

    /// <summary>The chosen editor is not installed where Vantage looks, so nothing was started.</summary>
    public sealed record EditorNotFound(EditorChoice Editor) : OpenInEditorOutcome;

    /// <summary>The editor was found but Windows refused to start it (access denied, a broken install); the reason is Windows' own.</summary>
    public sealed record EditorNotStarted(EditorChoice Editor, string Reason) : OpenInEditorOutcome;

    /// <summary>The folder was deleted since the row was listed, so nothing was started.</summary>
    public sealed record FolderMissing(string FolderName) : OpenInEditorOutcome;
}