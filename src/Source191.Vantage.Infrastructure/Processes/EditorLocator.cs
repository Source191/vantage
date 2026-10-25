using Source191.Vantage.Domain;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>The folders editors are installed under, and the PATH, so a test can point them at a temporary tree.</summary>
public sealed record EditorFolders(IReadOnlyList<string> PathFolders, string LocalAppData, string ProgramFiles, string ProgramFilesX86, string Windows)
{
    public static EditorFolders ForCurrentUser()
    {
        string[] path = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new EditorFolders(
            path,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.Windows));
    }
}

/// <summary>
/// Finds each preset's real executable. VS Code is found through the code.cmd shim on the PATH (the executable sits
/// two folders up, beside the shim's bin folder) or its usual install folders, but the shim itself is never answered,
/// because Windows would run it through cmd.exe.
/// </summary>
public sealed class EditorLocator(IFolders folders, IProcessRunner processes, EditorFolders where) : IEditorLocator
{
    public async Task<string?> LocateAsync(EditorPreset preset, CancellationToken cancellationToken)
    {
        return preset switch
        {
            EditorPreset.VsCode => FirstExisting([.. CodeBesideTheShim(), Path.Combine(where.LocalAppData, "Programs", "Microsoft VS Code", "Code.exe"), Path.Combine(where.ProgramFiles, "Microsoft VS Code", "Code.exe")]),
            EditorPreset.Cursor => FirstExisting([Path.Combine(where.LocalAppData, "Programs", "cursor", "Cursor.exe")]),
            EditorPreset.Rider => NewestRider(),
            EditorPreset.VisualStudio => await VisualStudioAsync(cancellationToken),
            EditorPreset.Explorer => Path.Combine(where.Windows, "explorer.exe"),
            EditorPreset.Custom => null,
        };
    }

    private IEnumerable<string> CodeBesideTheShim()
    {
        return where.PathFolders
            .Where(folder => folders.FileExists(Path.Combine(folder, "code.cmd")))
            .Select(folder => Path.Combine(folder, "..", "Code.exe"))
            .Select(Path.GetFullPath);
    }

    private string? FirstExisting(IEnumerable<string> candidates) => candidates.FirstOrDefault(folders.FileExists);

    private string? NewestRider()
    {
        string jetBrains = Path.Combine(where.ProgramFiles, "JetBrains");
        IEnumerable<string> installed = folders.DirectoryExists(jetBrains)
            ? folders.Directories(jetBrains).Select(install => Path.Combine(install, "bin", "rider64.exe"))
            : [];
        string toolbox = Path.Combine(where.LocalAppData, "JetBrains", "Toolbox", "apps");
        IEnumerable<string> managed = folders.DirectoryExists(toolbox)
            ? folders.Files(toolbox).Where(file => string.Equals(Path.GetFileName(file), "rider64.exe", StringComparison.OrdinalIgnoreCase))
            : [];

        return installed.Concat(managed)
            .Where(folders.FileExists)
            .OrderByDescending(file => file, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private async Task<string?> VisualStudioAsync(CancellationToken cancellationToken)
    {
        string vswhere = Path.Combine(where.ProgramFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!folders.FileExists(vswhere))
        {
            return null;
        }

        ProcessResult result = await processes.RunAsync(new ProcessRequest(vswhere, ["-latest", "-property", "productPath"], WorkingDirectory: null), cancellationToken);
        string? productPath = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();

        return result.ExitCode == 0 && productPath is not null && folders.FileExists(productPath) ? productPath : null;
    }
}