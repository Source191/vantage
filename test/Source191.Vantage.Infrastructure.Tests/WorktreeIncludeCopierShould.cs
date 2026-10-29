using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Uses a real temporary repository so that "gitignored" means what git says it means.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WorktreeIncludeCopierShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-include-" + Guid.NewGuid().ToString("N"));
    private readonly GitCli git = new(new ProcessRunner(), "git");

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

    /// <summary>
    /// git quotes a name with non-ASCII characters in its line-based listing ("Caf\303\251.local.json"), which no
    /// pattern matches, so this needs the listing's exact names.
    /// </summary>
    [Fact]
    public async Task CopyFilesWhoseNamesHaveNonAsciiCharactersSpacesAndApostrophes()
    {
        await CreateRepositoryAsync(
            gitignore: "*.local.json\n",
            worktreeInclude: "*.local.json\n",
            files: ["Café.local.json", "my app.local.json", "it's.local.json"]);

        IReadOnlyList<string> copied = await Copier.CopyAsync(Repository, Worktree, TestContext.Current.CancellationToken);

        copied.Order(StringComparer.Ordinal).ShouldBe(["Café.local.json", "it's.local.json", "my app.local.json"]);
        File.Exists(Path.Combine(Worktree, "Café.local.json")).ShouldBeTrue();
        File.Exists(Path.Combine(Worktree, "my app.local.json")).ShouldBeTrue();
        File.Exists(Path.Combine(Worktree, "it's.local.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task CopyNothingWhenThereIsNoIncludeFile()
    {
        await CreateRepositoryAsync(gitignore: "*.local.json\n", worktreeInclude: null, files: ["src/app.local.json"]);

        IReadOnlyList<string> copied = await Copier.CopyAsync(Repository, Worktree, TestContext.Current.CancellationToken);

        copied.ShouldBeEmpty();
    }

    /// <summary>
    /// Characterisation: git never lists a path outside the repository and the glob matcher drops such paths anyway,
    /// so this passes today; it fails if either stops being true.
    /// </summary>
    [Fact]
    public async Task SkipParentRelativePathsFromGit()
    {
        await CreateRepositoryAsync(gitignore: "*.local.json\n", worktreeInclude: "*\n**/*\n", files: ["app.local.json"]);
        await File.WriteAllTextAsync(Path.Combine(root, "outside.local.json"), "outside", TestContext.Current.CancellationToken);
        IReadOnlyList<string> outsideBefore = FilesOutsideTheWorktree();

        IReadOnlyList<string> copied = await CopierOverListing("../outside.local.json", "sub/../../outside.local.json", "app.local.json")
            .CopyAsync(Repository, Worktree, TestContext.Current.CancellationToken);

        copied.ShouldBe(["app.local.json"]);
        FilesOutsideTheWorktree().ShouldBe(outsideBefore);
    }

    [Fact]
    public async Task SkipRootedAndNetworkPathsFromGit()
    {
        await CreateRepositoryAsync(gitignore: "*.local.json\n", worktreeInclude: "*\n**/*\n", files: ["app.local.json"]);
        IReadOnlyList<string> outsideBefore = FilesOutsideTheWorktree();

        IReadOnlyList<string> copied = await CopierOverListing("C:/Users/me/secrets.local.json", "//server/share/secrets.local.json", "/secrets.local.json", "app.local.json")
            .CopyAsync(Repository, Worktree, TestContext.Current.CancellationToken);

        copied.ShouldBe(["app.local.json"]);
        File.Exists(Path.Combine(Worktree, "app.local.json")).ShouldBeTrue();
        FilesOutsideTheWorktree().ShouldBe(outsideBefore);
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

    /// <summary>A copier whose git answers the ignored-file listing with exactly these lines.</summary>
    private static WorktreeIncludeCopier CopierOverListing(params string[] listing)
    {
        FakeProcessRunner processes = new FakeProcessRunner().AnswerAll(new ProcessResult(0, string.Join('\0', listing) + "\0", string.Empty));

        return new WorktreeIncludeCopier(new GitCli(processes, "git"));
    }

    private IReadOnlyList<string> FilesOutsideTheWorktree()
    {
        return [.. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(file => !file.StartsWith(Worktree + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    private async Task RunGitAsync(params string[] arguments)
    {
        ProcessResult result = await new ProcessRunner().RunAsync(new ProcessRequest("git", arguments, Repository), TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.StandardError);
    }
}