using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// The two programs the console and <c>vantage here</c> cannot start without: both full paths, or the sentence that says
/// why one cannot be run, which is printed once and answered with exit code 1.
/// </summary>
public abstract record RequiredPrograms
{
    public static RequiredPrograms Check(ClaudeLocation claude, ProgramLocation git)
    {
        ArgumentNullException.ThrowIfNull(claude);
        ArgumentNullException.ThrowIfNull(git);

        return new Found(((ClaudeLocation.Found)claude).Path, ((ProgramLocation.Found)git).Path);
    }

    public sealed record Found(string Claude, string Git) : RequiredPrograms;

    public sealed record Missing(string Sentence) : RequiredPrograms;
}
