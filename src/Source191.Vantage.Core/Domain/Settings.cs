namespace Source191.Vantage.Domain;

/// <summary>
/// Vantage's settings, complete and immutable: where repositories live, where new worktrees go, which editor opens a
/// folder and how new worktrees branch. Whatever a file leaves out has been resolved to its default by <see cref="Resolve"/>.
/// </summary>
public sealed record Settings
{
    private Settings(IReadOnlyList<FolderPath> repositoryRoots, FolderPath worktreeRoot, EditorChoice editor, WorktreeBase defaultBase, bool checkForUpdates)
    {
        RepositoryRoots = repositoryRoots;
        WorktreeRoot = worktreeRoot;
        Editor = editor;
        DefaultBase = defaultBase;
        CheckForUpdates = checkForUpdates;
    }

    public IReadOnlyList<FolderPath> RepositoryRoots { get; }

    public FolderPath WorktreeRoot { get; }

    public EditorChoice Editor { get; }

    public WorktreeBase DefaultBase { get; }

    /// <summary>Whether Vantage asks nuget.org, at most once a day, for a newer release of itself.</summary>
    public bool CheckForUpdates { get; }

    /// <summary>
    /// Fills in what is missing: no roots means <c>&lt;home&gt;\source\repos</c>; no worktree root means <c>worktrees</c>
    /// beside the first root (inside it when it has no parent, such as <c>D:\</c>); no base means origin's default branch; no update setting means the check is on.
    /// </summary>
    public static Settings Resolve(IReadOnlyList<FolderPath>? roots, FolderPath? worktreeRoot, EditorChoice editor, WorktreeBase? defaultBase, FolderPath home, bool? checkForUpdates = null)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(home);
        IReadOnlyList<FolderPath> resolvedRoots = roots is { Count: > 0 } ? roots : [home.Combine(Path.Combine("source", "repos"))];
        FolderPath first = resolvedRoots[0];

        return new Settings(resolvedRoots, worktreeRoot ?? (first.Parent ?? first).Combine("worktrees"), editor, defaultBase ?? WorktreeBase.RemoteDefault, checkForUpdates ?? true);
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

        SettingsProblem? problem = roots switch
        {
            [] => new SettingsProblem.NoRoots(),
            _ when roots.FirstOrDefault(root => !folderExists(root.Value)) is FolderPath missing => new SettingsProblem.RootMissing(missing),
            _ => null,
        };

        return problem is null ? Validated(text, roots) : Refused(problem);
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
            CheckForUpdates);
    }

    public bool Equals(Settings? other)
    {
        return other is not null
            && RepositoryRoots.SequenceEqual(other.RepositoryRoots)
            && WorktreeRoot.Equals(other.WorktreeRoot)
            && Editor.Equals(other.Editor)
            && DefaultBase == other.DefaultBase
            && CheckForUpdates == other.CheckForUpdates;
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
            return new SettingsValidation.Valid(new Settings(roots, worktreeRoot, EditorChoice.Of(text.Preset), text.Base, text.CheckForUpdates));
        }

        IReadOnlyList<string> arguments = CommandArguments.Split(text.Arguments);

        return EditorChoice.Problem(text.Executable, arguments) is SettingsProblem problem
            ? Refused(problem)
            : new SettingsValidation.Valid(new Settings(roots, worktreeRoot, EditorChoice.Custom(text.Executable, arguments), text.Base, text.CheckForUpdates));
    }

    private static SettingsValidation.Refused Refused(SettingsProblem problem) => new(problem);
}

/// <summary>The settings dialog's fields as typed, before any rule is applied.</summary>
public sealed record SettingsText(string Roots, string WorktreeRoot, EditorPreset Preset, string Executable, string Arguments, WorktreeBase Base, bool CheckForUpdates = true);

/// <summary>Why typed settings are refused, as a fact; <c>Wording</c> says it.</summary>
public abstract record SettingsProblem
{
    public sealed record NoRoots : SettingsProblem;

    public sealed record RootNotFullPath(string Text) : SettingsProblem;

    public sealed record RootMissing(FolderPath Path) : SettingsProblem;

    public sealed record WorktreeRootNotFullPath : SettingsProblem;

    public sealed record NoExecutable : SettingsProblem;

    public sealed record BatchFileExecutable : SettingsProblem;

    public sealed record NoFolderPlaceholder : SettingsProblem;
}

/// <summary>Typed settings checked against the rules: the settings, or the reason they are refused.</summary>
public abstract record SettingsValidation
{
    public sealed record Valid(Settings Settings) : SettingsValidation;

    public sealed record Refused(SettingsProblem Problem) : SettingsValidation;
}