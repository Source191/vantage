using Source191.Torni.Ports;
using System.Diagnostics;

namespace Source191.Torni.Application;

/// <summary>
/// The two programs the console and <c>torni here</c> cannot start without: both full paths, or the sentence that says
/// why one cannot be run, which is printed once and answered with exit code 1.
/// </summary>
public abstract record RequiredPrograms
{
    public static RequiredPrograms Check(ClaudeLocation claude, ProgramLocation git)
    {
        // Claude Code's problem is said before git's, as the console has always said it.
        return (claude, git) switch
        {
            (ClaudeLocation.BatchFileOnly batchFile, _) => new Missing(Wording.ClaudeIsABatchFile(batchFile.Path)),
            (ClaudeLocation.Missing, _) => new Missing(Wording.ClaudeNotFound),
            (ClaudeLocation.Found, ProgramLocation.Missing) => new Missing(Wording.GitNotFound),
            (ClaudeLocation.Found foundClaude, ProgramLocation.Found foundGit) => new Found(foundClaude.Path, foundGit.Path),
            _ => throw new UnreachableException(),
        };
    }

    public sealed record Found(string Claude, string Git) : RequiredPrograms;

    public sealed record Missing(string Sentence) : RequiredPrograms;
}