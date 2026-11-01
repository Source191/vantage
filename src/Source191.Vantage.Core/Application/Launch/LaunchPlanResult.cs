namespace Source191.Vantage.Application.Launch;

/// <summary>A plan, or the reason there is none.</summary>
public abstract record LaunchPlanResult
{
    public sealed record Planned(LaunchPlan Plan) : LaunchPlanResult;

    public sealed record Refused(LaunchProblem Problem) : LaunchPlanResult;
}