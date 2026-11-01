using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Application.Views;

/// <summary>Why the wizard will not go on, as a fact; <see cref="Wording"/> says it.</summary>
public abstract record WizardProblem
{
    /// <summary>Nothing is wrong: the problem line is empty.</summary>
    public sealed record None : WizardProblem;

    public sealed record NoRepository : WizardProblem;

    /// <summary>The plan builder said no, for its own reason.</summary>
    public sealed record Refused(LaunchProblem Problem) : WizardProblem;
}