namespace Source191.Vantage.Domain;

/// <summary>What one running process uses: when it started (UTC), its private working set, and the command line it was started with.</summary>
public sealed record ProcessUsage(DateTimeOffset StartedAt, MemorySize PrivateMemory, string CommandLine);