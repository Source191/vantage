namespace Source191.Vantage.Domain;

/// <summary>A custom editor's command checked against the rules: the editor, or the first reason it cannot be used.</summary>
public abstract record EditorChoiceValidation
{
    public sealed record Valid(EditorChoice Editor) : EditorChoiceValidation;

    public sealed record Refused(SettingsProblem Problem) : EditorChoiceValidation;
}