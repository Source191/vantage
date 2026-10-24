using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>
/// A throwaway folder under the temp directory that plays the part of a repository root, with real git repositories
/// inside it. Deleting it clears git's read-only object files first, which Directory.Delete would otherwise refuse.
/// </summary>
public sealed class TemporaryGitRoot : IDisposable
{
    private readonly ProcessRunner processes = new();

    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vantage-root-" + Guid.NewGuid().ToString("N"));

    public TemporaryGitRoot()
    {
        Directory.CreateDirectory(Path);
    }

    public async Task<string> CreateRepositoryAsync(string name)
    {
        string repository = System.IO.Path.Combine(Path, name);
        Directory.CreateDirectory(repository);
        await GitAsync(repository, "init", "-q", "-b", "main");
        await GitAsync(repository, "config", "user.email", "vantage@example.test");
        await GitAsync(repository, "config", "user.name", "Vantage Tests");
        await File.WriteAllTextAsync(System.IO.Path.Combine(repository, "README.md"), "# test");
        await GitAsync(repository, "add", "-A");
        await GitAsync(repository, "commit", "-q", "-m", "init");

        return repository;
    }

    public async Task<string> AddSiblingWorktreeAsync(string repository, string branch)
    {
        string worktree = $"{repository}-{branch}";
        await GitAsync(repository, "worktree", "add", worktree, "-b", branch, "main");

        return worktree;
    }

    public async Task CommitAFileAsync(string checkout, string fileName)
    {
        await File.WriteAllTextAsync(System.IO.Path.Combine(checkout, fileName), fileName);
        await GitAsync(checkout, "add", "-A");
        await GitAsync(checkout, "commit", "-q", "-m", fileName);
    }

    public async Task GitAsync(string directory, params string[] arguments)
    {
        ProcessResult result = await processes.RunAsync(new ProcessRequest("git", arguments, directory), CancellationToken.None);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(result.StandardError);
        }
    }

    public static void DeleteTree(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(folder, recursive: true);
    }

    public void Dispose()
    {
        DeleteTree(Path);
    }
}