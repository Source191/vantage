namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>What <c>vantage setup-terminal</c> was asked to do; a null sound means "use the default".</summary>
public sealed record TerminalSetupRequest(string? Sound, string? SessionSound, bool Yes, bool Remove);

/// <summary>Why setup-terminal will not run, as a fact; <see cref="Wording"/> says it.</summary>
public abstract record TerminalSetupProblem
{
    public sealed record RemoveWithSound : TerminalSetupProblem;

    public sealed record SoundNotFound(string Path) : TerminalSetupProblem;

    public sealed record SoundNotWav(string Path) : TerminalSetupProblem;
}