namespace Source191.Torni.Infrastructure.Tests;

/// <summary>
/// A test class that sets or reads a process environment variable another test could set joins this collection, which
/// runs alone: no other class runs while it holds the Claude markers.
/// </summary>
[CollectionDefinition("Process environment", DisableParallelization = true)]
public sealed class ProcessEnvironmentDefinition;