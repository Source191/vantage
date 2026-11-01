namespace Source191.Vantage.Domain;

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