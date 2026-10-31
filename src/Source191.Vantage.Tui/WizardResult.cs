using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Tui;

/// <summary>What the wizard produces: a validated plan, started and opened in its own tab.</summary>
public sealed record WizardResult(LaunchPlan Plan);