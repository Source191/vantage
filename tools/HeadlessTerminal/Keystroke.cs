namespace Source191.Vantage.HeadlessTerminal;

/// <summary>One scripted keystroke batch: when to send it (milliseconds from the start) and what to send.</summary>
public sealed record Keystroke(int At, string Keys);