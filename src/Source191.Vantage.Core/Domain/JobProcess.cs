namespace Source191.Vantage.Domain;

/// <summary>A supervised job's process: running with its id, or none, because the job is stopped.</summary>
public abstract record JobProcess
{
    public sealed record Running(int Id) : JobProcess;

    public sealed record None : JobProcess;
}