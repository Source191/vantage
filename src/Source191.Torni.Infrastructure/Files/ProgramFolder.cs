namespace Source191.Torni.Adapters.Files;

/// <summary>
/// The folder the console's own files sit in (<c>CHANGELOG.md</c>, <c>logo.ico</c>). winget puts a portable package in a
/// folder of its own and adds a symbolic link to it in a links folder on the PATH; started through that link, .NET answers
/// the link's folder for <see cref="AppContext.BaseDirectory"/> (checked 3 November 2026, windows.md), so a process
/// started through a link reads beside the link's final target. Anything else (a program started directly, or a process path that cannot be read) keeps the
/// base directory.
/// </summary>
public static class ProgramFolder
{
    public static string Of(string baseDirectory, string? processPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        if (string.IsNullOrEmpty(processPath))
        {
            return baseDirectory;
        }

        try
        {
            FileSystemInfo? target = File.ResolveLinkTarget(processPath, returnFinalTarget: true);

            return target is null ? baseDirectory : Path.GetDirectoryName(target.FullName) ?? baseDirectory;
        }
        catch (IOException)
        {
            return baseDirectory;
        }
        catch (UnauthorizedAccessException)
        {
            return baseDirectory;
        }
    }
}