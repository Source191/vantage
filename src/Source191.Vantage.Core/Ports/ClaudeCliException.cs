namespace Source191.Vantage.Ports;

/// <summary>Claude Code exited with an error; the message is its own sentence, never empty.</summary>
public sealed class ClaudeCliException(string message) : Exception(message);

