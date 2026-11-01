using Source191.Vantage.Domain;

namespace Source191.Vantage.Application;

/// <summary>One program a session started, with everything under it: an MCP server, a shell, whatever else.</summary>
public sealed record MemoryShare(string Label, MemorySize Memory);