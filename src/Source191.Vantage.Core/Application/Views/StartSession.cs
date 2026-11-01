using Source191.Vantage.Application.Launch;

namespace Source191.Vantage.Application.Views;

/// <summary>Start the session the launch wizard planned, from either view, and open its tab when asked.</summary>
public sealed record StartSession(LaunchPlan Plan) : ViewEffect;