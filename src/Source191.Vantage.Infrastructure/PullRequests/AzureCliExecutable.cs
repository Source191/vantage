using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Adapters.PullRequests;

/// <summary>
/// Finds az.cmd on the PATH;
/// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">CreateProcess
/// cannot find a .cmd by its bare name</see>.
/// </summary>
public static class AzureCliExecutable
{
    /// <summary>The first az.cmd in the PATH's folders, in order.</summary>
    public static ProgramLocation Locate(IReadOnlyList<string> pathFolders)
    {
        ArgumentNullException.ThrowIfNull(pathFolders);
        string? found = PathFolders.Searchable(pathFolders)
            .Select(folder => Path.Combine(folder, "az.cmd"))
            .FirstOrDefault(File.Exists);

        return found is null ? new ProgramLocation.Missing() : new ProgramLocation.Found(found);
    }
}