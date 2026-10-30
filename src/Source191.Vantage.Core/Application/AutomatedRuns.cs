using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>How many automated runs the listing held and the memory of their process trees together, for the counts.</summary>
public sealed record AutomatedRuns(int Count, MemorySize Memory)
{
    public static AutomatedRuns None { get; } = new(0, MemorySize.Zero);
}