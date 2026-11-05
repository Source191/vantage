namespace Source191.Torni.Ports;

/// <summary>A commit no remote has: its short hash, when it was committed and its subject line.</summary>
public sealed record UnpushedCommit(string Hash, DateTimeOffset At, string Subject);