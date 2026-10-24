using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Tui;

/// <summary>What the wizard produces: a validated plan and whether to open the session in its own tab as soon as it starts.</summary>
public sealed record WizardResult(LaunchPlan Plan, bool OpenInTab);