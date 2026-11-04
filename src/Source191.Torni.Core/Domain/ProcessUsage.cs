namespace Source191.Torni.Domain;

/// <summary>What one running process uses: when it started (UTC), its private working set, and the label of the program it runs (its command line is reduced to the label by the adapter and never kept).</summary>
public sealed record ProcessUsage(DateTimeOffset StartedAt, MemorySize PrivateMemory, string Label);