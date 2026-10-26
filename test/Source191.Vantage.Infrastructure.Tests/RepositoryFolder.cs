namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>This repository's own folder, for the tests that read its documents or need a trusted working folder.</summary>
internal static class RepositoryFolder
{
    /// <summary>The folder holding global.json, found by walking up from the test's output folder.</summary>
    public static string Root()
    {
        DirectoryInfo? folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "global.json")))
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException($"No global.json above {AppContext.BaseDirectory}.");
    }
}