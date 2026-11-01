namespace Source191.Vantage.Domain;

/// <summary>
/// The memory a session's process tree uses, or nothing to measure: the session has no process, or it has gone. Never
/// <see cref="MemorySize.Zero"/>, which would read as a measured figure.
/// </summary>
public abstract record SessionMemory
{
    public sealed record Measured(MemorySize Size) : SessionMemory;

    public sealed record NothingToMeasure : SessionMemory;
}