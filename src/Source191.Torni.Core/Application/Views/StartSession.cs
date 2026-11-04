using Source191.Torni.Application.Launch;

namespace Source191.Torni.Application.Views;

/// <summary>Start the session the launch wizard planned, from any tab, and open its tab once the supervisor lists it.</summary>
public sealed record StartSession(LaunchPlan Plan) : ViewEffect;