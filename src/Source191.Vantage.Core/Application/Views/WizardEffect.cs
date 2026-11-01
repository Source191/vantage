using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Application.Views;

/// <summary>What the dialog does after a reduce: nothing, start the plan, or close.</summary>
public abstract record WizardEffect
{
    /// <summary>Nothing: the dialog pushes the new state back to its controls.</summary>
    public sealed record None : WizardEffect;

    public sealed record Launch(LaunchPlan Plan) : WizardEffect;

    public sealed record Close : WizardEffect;
}