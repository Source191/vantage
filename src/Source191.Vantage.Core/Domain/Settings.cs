using System.Diagnostics;

namespace Source191.Vantage.Domain;

/// <summary>
/// Vantage's settings, complete and immutable: where repositories live, where new worktrees go, which editor opens a
/// folder, how new worktrees branch and where the wizard starts a new session. Whatever a file leaves out has been resolved to its default by <see cref="Resolve"/>.
/// </summary>
public sealed record Settings
{
    private Settings(IReadOnlyList<FolderPath> repositoryRoots, FolderPath worktreeRoot, EditorChoice editor, WorktreeBase defaultBase, bool checkForUpdates, LaunchMode defaultPlace)
    {
        RepositoryRoots = repositoryRoots;
        WorktreeRoot = worktreeRoot;
        Editor = editor;
        DefaultBase = defaultBase;
        CheckForUpdates = checkForUpdates;
        DefaultPlace = defaultPlace;
    }

    public IReadOnlyList<FolderPath> RepositoryRoots { get; }

    public FolderPath WorktreeRoot { get; }

    public EditorChoice Editor { get; }

    public WorktreeBase DefaultBase { get; }

    /// <summary>Whether Vantage asks nuget.org, at most once a day, for a newer release of itself.</summary>
    public bool CheckForUpdates { get; }

    /// <summary>Where the wizard opened with n starts a new session: a new worktree, or the repository's current branch.</summary>
    public LaunchMode DefaultPlace { get; }

    /// <summary>The base a file without one branches from: origin's default branch.</summary>
    public const WorktreeBase BaseWhenUnset = WorktreeBase.RemoteDefault;

    /// <summary>A file without the update setting has the check on.</summary>
    public const bool ChecksForUpdatesWhenUnset = true;

    /// <summary>A file without a place starts new sessions in a new worktree.</summary>
    public const LaunchMode PlaceWhenUnset = LaunchMode.NewWorktree;

    /// <summary>
    /// Fills in what is missing: no roots means <c>&lt;home&gt;\source\repos</c>; <see cref="WorktreeRootChoice.BesideFirstRoot"/> means <c>worktrees</c>
    /// beside the first root (inside it when it has no parent, such as <c>D:\</c>). A file's missing base, update setting and place are <see cref="BaseWhenUnset"/>, <see cref="ChecksForUpdatesWhenUnset"/> and <see cref="PlaceWhenUnset"/>.
    /// </summary>
    public static Settings Resolve(IReadOnlyList<FolderPath> roots, WorktreeRootChoice worktreeRoot, EditorChoice editor, WorktreeBase defaultBase, FolderPath home, bool checkForUpdates = ChecksForUpdatesWhenUnset, LaunchMode defaultPlace = PlaceWhenUnset)
    {
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentNullException.ThrowIfNull(worktreeRoot);
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(home);
        IReadOnlyList<FolderPath> resolvedRoots = roots is { Count: > 0 } ? roots : [home.Combine(Path.Combine("source", "repos"))];
        FolderPath first = resolvedRoots[0];
        FolderPath resolvedWorktreeRoot = worktreeRoot switch
        {
            WorktreeRootChoice.At chosen => chosen.Folder,
            WorktreeRootChoice.BesideFirstRoot => (first.Parent ?? first).Combine("worktrees"),
            _ => throw new UnreachableException(),
        };

        return new Settings(resolvedRoots, resolvedWorktreeRoot, editor, defaultBase, checkForUpdates, defaultPlace);
    }

    /// <summary>
    /// Checks what the settings dialog holds, in the order its fields read: the roots (one per line, each a full path
    /// that exists), the worktree root (a full path, created when first used), then a custom editor's command.
    /// </summary>
    public static SettingsValidation Validate(SettingsText text, Func<string, bool> folderExists)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(folderExists);
        List<string> lines = [.. text.Roots.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)];
        List<FolderPath> roots = [];
        foreach (string line in lines)
        {
            if (!FolderPath.TryParse(line, out FolderPath? root))
            {
                return Refused(new SettingsProblem.RootNotFullPath(line));
            }

            roots.Add(root);
        }

        return roots switch
        {
            [] => Refused(new SettingsProblem.NoRoots()),
            _ when roots.FirstOrDefault(root => !folderExists(root.Value)) is FolderPath missing => Refused(new SettingsProblem.RootMissing(missing)),
            _ when roots.Where((root, index) => roots.Take(index).Contains(root)).FirstOrDefault() is FolderPath repeated => Refused(new SettingsProblem.RootAlreadyListed(repeated)),
            _ => Validated(text, roots),
        };
    }

    /// <summary>The settings as the dialog shows them: roots one per line and the custom arguments on one line.</summary>
    public SettingsText ToText()
    {
        return new SettingsText(
            string.Join('\n', RepositoryRoots.Select(root => root.Value)),
            WorktreeRoot.Value,
            Editor.Preset,
            Editor.Executable,
            CommandArguments.Join(Editor.Arguments),
            DefaultBase,
            CheckForUpdates,
            DefaultPlace);
    }

    /// <summary>These settings with the update check turned the other way.</summary>
    public Settings WithUpdateCheckToggled() => new(RepositoryRoots, WorktreeRoot, Editor, DefaultBase, !CheckForUpdates, DefaultPlace);

    public bool Equals(Settings? other)
    {
        return other is not null
            && RepositoryRoots.SequenceEqual(other.RepositoryRoots)
            && WorktreeRoot.Equals(other.WorktreeRoot)
            && Editor.Equals(other.Editor)
            && DefaultBase == other.DefaultBase
            && CheckForUpdates == other.CheckForUpdates
            && DefaultPlace == other.DefaultPlace;
    }

    public override int GetHashCode() => HashCode.Combine(RepositoryRoots.Count, WorktreeRoot, Editor, DefaultBase);

    private static SettingsValidation Validated(SettingsText text, IReadOnlyList<FolderPath> roots)
    {
        if (!FolderPath.TryParse(text.WorktreeRoot.Trim(), out FolderPath? worktreeRoot))
        {
            return Refused(new SettingsProblem.WorktreeRootNotFullPath());
        }

        if (text.Preset != EditorPreset.Custom)
        {
            return new SettingsValidation.Valid(new Settings(roots, worktreeRoot, EditorChoice.Of(text.Preset), text.Base, text.CheckForUpdates, text.Place));
        }

        IReadOnlyList<string> arguments = CommandArguments.Split(text.Arguments);

        return EditorChoice.Validate(text.Executable, arguments) switch
        {
            EditorChoiceValidation.Valid valid => new SettingsValidation.Valid(new Settings(roots, worktreeRoot, valid.Editor, text.Base, text.CheckForUpdates, text.Place)),
            EditorChoiceValidation.Refused refused => Refused(refused.Problem),
            _ => throw new UnreachableException(),
        };
    }

    private static SettingsValidation.Refused Refused(SettingsProblem problem) => new(problem);
}

/// <summary>The settings dialog's fields as typed, before any rule is applied.</summary>
public sealed record SettingsText(string Roots, string WorktreeRoot, EditorPreset Preset, string Executable, string Arguments, WorktreeBase Base, bool CheckForUpdates = true, LaunchMode Place = LaunchMode.NewWorktree);

/// <summary>Why typed settings are refused, as a fact; <c>Wording</c> says it.</summary>
public abstract record SettingsProblem
{
    public sealed record NoRoots : SettingsProblem;

    public sealed record RootNotFullPath(string Text) : SettingsProblem;

    public sealed record RootMissing(FolderPath Path) : SettingsProblem;

    public sealed record RootAlreadyListed(FolderPath Path) : SettingsProblem;

    public sealed record WorktreeRootNotFullPath : SettingsProblem;

    public sealed record NoExecutable : SettingsProblem;

    /// <summary>
    /// A bare name or a relative path
    /// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">would be
    /// searched for in the folder Vantage was started from</see>.
    /// </summary>
    public sealed record ExecutableNotFullPath : SettingsProblem;

    public sealed record BatchFileExecutable : SettingsProblem;

    public sealed record NoFolderPlaceholder : SettingsProblem;
}

/// <summary>Typed settings checked against the rules: the settings, or the reason they are refused.</summary>
public abstract record SettingsValidation
{
    public sealed record Valid(Settings Settings) : SettingsValidation;

    public sealed record Refused(SettingsProblem Problem) : SettingsValidation;
}