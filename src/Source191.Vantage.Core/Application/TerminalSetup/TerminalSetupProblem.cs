namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>Why setup-terminal will not run, as a fact; <see cref="Wording"/> says it.</summary>
public abstract record TerminalSetupProblem
{
    public sealed record RemoveWithSound : TerminalSetupProblem;

    public sealed record SoundNotFound(string Path) : TerminalSetupProblem;

    public sealed record SoundNotWav(string Path) : TerminalSetupProblem;
}