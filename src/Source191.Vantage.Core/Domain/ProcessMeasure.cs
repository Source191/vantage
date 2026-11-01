namespace Source191.Vantage.Domain;

/// <summary>What a member of a process tree uses, or that it refused to be measured (it counts as nothing).</summary>
public abstract record ProcessMeasure
{
    public sealed record Measured(ProcessUsage Usage) : ProcessMeasure;

    public sealed record Unmeasured : ProcessMeasure;
}