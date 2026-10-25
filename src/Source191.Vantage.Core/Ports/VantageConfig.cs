using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>
/// config.json as written: every member may be missing, as it is in a file from an earlier version or one edited by
/// hand, and <c>SettingsFile</c> resolves what is missing to its default. A file written by this version names all five.
/// </summary>
public sealed record VantageConfig(IReadOnlyList<string>? RepositoryRoots, WorktreeBase? DefaultBase, string? WorktreeRoot, EditorSetting? Editor, bool? CheckForUpdates = null)
{
    public bool Equals(VantageConfig? other)
    {
        return other is not null
            && DefaultBase == other.DefaultBase
            && string.Equals(WorktreeRoot, other.WorktreeRoot, StringComparison.Ordinal)
            && Equals(Editor, other.Editor)
            && CheckForUpdates == other.CheckForUpdates
            && (RepositoryRoots ?? []).SequenceEqual(other.RepositoryRoots ?? []);
    }

    public override int GetHashCode() => HashCode.Combine(DefaultBase, WorktreeRoot, CheckForUpdates, RepositoryRoots?.Count);
}

/// <summary>The editor as config.json holds it: <c>{ "preset": "VsCode" }</c>, or a custom executable and its argument list.</summary>
public sealed record EditorSetting(string? Preset, string? Executable, IReadOnlyList<string>? Arguments)
{
    public bool Equals(EditorSetting? other)
    {
        return other is not null
            && string.Equals(Preset, other.Preset, StringComparison.Ordinal)
            && string.Equals(Executable, other.Executable, StringComparison.Ordinal)
            && (Arguments ?? []).SequenceEqual(other.Arguments ?? []);
    }

    public override int GetHashCode() => HashCode.Combine(Preset, Executable);
}