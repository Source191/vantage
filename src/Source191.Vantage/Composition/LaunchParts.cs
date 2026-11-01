using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Git;
using Source191.Vantage.Application;
using Source191.Vantage.Application.Launch;
using System.Diagnostics;

namespace Source191.Vantage.Composition;

/// <summary>What the console and <c>vantage here</c> both build once Claude Code and git are found.</summary>
internal sealed record LaunchParts(RequiredPrograms.Found Programs, ClaudeCli Claude, GitCli Git, LaunchService Launches)
{
    // Claude Code and git, found once for the console and vantage here; the sentence and exit code 1 when either cannot be run.
    public static async Task<int> RunAsync(SharedParts parts, TextWriter error, Func<LaunchParts, Task<int>> run)
    {
        switch (RequiredPrograms.Check(ClaudeExecutable.Locate(), parts.Programs.Locate("git")))
        {
            case RequiredPrograms.Found found:
                ClaudeCli claude = new(parts.Processes, found.Claude);
                GitCli git = new(parts.Processes, found.Git);
                LaunchService launchService = new(claude, git, parts.Store, parts.Store, new WorktreeIncludeCopier(git), TimeProvider.System);
                return await run(new LaunchParts(found, claude, git, launchService));
            case RequiredPrograms.Missing missing:
                await error.WriteLineAsync(missing.Sentence);
                return 1;
            default:
                throw new UnreachableException();
        }
    }
}