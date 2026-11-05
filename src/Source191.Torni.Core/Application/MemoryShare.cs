using Source191.Torni.Domain;

namespace Source191.Torni.Application;

/// <summary>One program a session started, with everything under it: an MCP server, a shell, whatever else.</summary>
public sealed record MemoryShare(string Label, MemorySize Memory);