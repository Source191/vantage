using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Uses a real temporary repository so that "gitignored" means what git says it means.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WorktreeIncludeCopierShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-include-" + Guid.NewGuid().ToString("N"));
    private readonly GitCli git = new(new ProcessRunner());

    private string Repository => Path.Combine(root, "Northwind.Orders");

    private string Worktree => Path.Combine(root, "Northwind.Orders-fix");

    private WorktreeIncludeCopier Copier => new(git);

    [Fact]
    public async Task CopyOnlyGitignoredFilesThatMatchAPattern()
    {
        await CreateRepositoryAsync(
            gitignore: "*.local.json\nsecrets/\n",
            worktreeInclude: "# local configuration\n\n*.local.json\nsecrets/key.txt\nnotes.txt\n",
            files: ["src/app.local.json", "src/app.json", "secrets/key.txt", "secrets/other.txt", "notes.txt"]);

        IReadOnlyList<string> copied = await Copier.CopyAsync(Repository, Worktree, TestContext.Current.CancellationToken);

        copied.Order().ShouldBe(["secrets/key.txt", "src/app.local.json"]);
        File.Exists(Path.Combine(Worktree, "src", "app.local.json")).ShouldBeTrue();
        File.Exists(Path.Combine(Worktree, "secrets", "key.txt")).ShouldBeTrue();
        File.Exists(Path.Combine(Worktree, "src", "app.json")).ShouldBeFalse();
        File.Exists(Path.Combine(Worktree, "notes.txt")).ShouldBeFalse();
    }

    [Fact]
    public async Task CopyNothingWhenThereIsNoIncludeFile()
    {
        await CreateRepositoryAsync(gitignore: "*.local.json\n", worktreeInclude: null, files: ["src/app.local.json"]);

        IReadOnlyList<string> copied = await Copier.CopyAsync(Repository, Worktree, TestContext.Current.CancellationToken);

        copied.ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(root, recursive: true);
    }

    private async Task CreateRepositoryAsync(string gitignore, string? worktreeInclude, IReadOnlyList<string> files)
    {
        Directory.CreateDirectory(Repository);
        Directory.CreateDirectory(Worktree);
        await File.WriteAllTextAsync(Path.Combine(Repository, ".gitignore"), gitignore, TestContext.Current.CancellationToken);
        if (worktreeInclude is not null)
        {
            await File.WriteAllTextAsync(Path.Combine(Repository, ".worktreeinclude"), worktreeInclude, TestContext.Current.CancellationToken);
        }

        foreach (string file in files)
        {
            string full = Path.Combine(Repository, file.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await File.WriteAllTextAsync(full, file, TestContext.Current.CancellationToken);
        }

        await RunGitAsync("init", "-q", "-b", "main");
    }

    private async Task RunGitAsync(params string[] arguments)
    {
        ProcessResult result = await new ProcessRunner().RunAsync(new ProcessRequest("git", arguments, Repository), TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.StandardError);
    }
}