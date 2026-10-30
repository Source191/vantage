namespace Source191.Vantage.Application.Views;

/// <summary>Which one setting a dialog edits, and the field it opens on; the Config tab raises one per setting row.</summary>
public abstract record SettingsScope(SettingsField Field)
{
    /// <summary>Every repository root, one per line, to change, add or remove together.</summary>
    public sealed record Roots() : SettingsScope(SettingsField.Roots);

    public sealed record WorktreeRoot() : SettingsScope(SettingsField.WorktreeRoot);

    public sealed record Editor() : SettingsScope(SettingsField.Editor);

    public sealed record Base() : SettingsScope(SettingsField.Base);

    /// <summary>The scope that edits a setting row; the update check and the setup rows have no dialog.</summary>
    public static SettingsScope For(ConfigRow row)
    {
        return row switch
        {
            ConfigRow.RepositoryRoot => new Roots(),
            ConfigRow.WorktreeRoot => new WorktreeRoot(),
            ConfigRow.OpenFolderWith => new Editor(),
            ConfigRow.DefaultBase => new Base(),
            _ => throw new ArgumentException($"A {row?.GetType().Name} row has no dialog.", nameof(row)),
        };
    }
}