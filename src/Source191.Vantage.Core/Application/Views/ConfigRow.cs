using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>
/// A row of the Config tab: a setting Vantage keeps in config.json, or a piece of setup it reads but does not edit.
/// Each case carries the facts its words are made from; <c>Wording</c> names the row and says its value.
/// </summary>
/// <param name="Key">The selection key, stable across a save so the row just edited stays selected.</param>
public abstract record ConfigRow(string Key)
{
    public sealed record RepositoryRoot(int Index, FolderPath Path) : ConfigRow($"root:{Index}");

    public sealed record WorktreeRoot(FolderPath Path) : ConfigRow("worktree-root");

    public sealed record OpenFolderWith(EditorChoice Editor) : ConfigRow("open-folder-with");

    public sealed record DefaultBase(WorktreeBase Base) : ConfigRow("default-base");

    public sealed record DefaultPlace(LaunchMode Place) : ConfigRow("default-place");

    public sealed record CheckForUpdates(bool On) : ConfigRow("check-for-updates");

    /// <summary>The heading over the setup rows: a row the selection steps over and any filter hides.</summary>
    public sealed record Heading() : ConfigRow("heading:setup");

    /// <summary>A piece of setup: what it is and what was read about it.</summary>
    public sealed record Setup(SetupItem Item, SetupState State) : ConfigRow($"setup:{Item}");
}