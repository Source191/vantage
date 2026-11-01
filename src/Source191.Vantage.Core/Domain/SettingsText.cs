namespace Source191.Vantage.Domain;

/// <summary>The settings dialog's fields as typed, before any rule is applied.</summary>
public sealed record SettingsText(string Roots, string WorktreeRoot, EditorPreset Preset, string Executable, string Arguments, WorktreeBase Base, bool CheckForUpdates = Settings.ChecksForUpdatesWhenUnset, LaunchMode Place = Settings.PlaceWhenUnset);