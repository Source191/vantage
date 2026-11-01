using Source191.Vantage.Application;
using Source191.Vantage.Domain;

namespace Source191.Vantage.TestDoubles;

/// <summary>Settings for tests: the given roots with every other setting at its default, held in memory.</summary>
public static class TestSettings
{
    public static FolderPath Home { get; } = FolderPath.From(@"C:\Users\me");

    public static Settings For(params string[] roots) => Settings.Resolve([.. roots.Select(root => FolderPath.From(root))], new WorktreeRootChoice.BesideFirstRoot(), EditorChoice.VsCode, WorktreeBase.RemoteDefault, Home);

    public static Settings For(string[] roots, string worktreeRoot, WorktreeBase defaultBase = WorktreeBase.RemoteDefault) => Settings.Resolve([.. roots.Select(root => FolderPath.From(root))], new WorktreeRootChoice.At(FolderPath.From(worktreeRoot)), EditorChoice.VsCode, defaultBase, Home);

    public static CurrentSettings Current(params string[] roots) => new(For(roots), new InMemoryState());

    public static CurrentSettings Current(Settings settings) => new(settings, new InMemoryState());
}