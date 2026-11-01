namespace Source191.Vantage.HeadlessTerminal;

/// <summary>A reply the pretend terminal owes the program, and how long to hold it back.</summary>
public sealed record TerminalReply(string Text, int DelayMs);