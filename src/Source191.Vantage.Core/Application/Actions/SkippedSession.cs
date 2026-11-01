namespace Source191.Vantage.Application.Actions;

/// <summary>A session close-empty left alone because it has messages by the time it runs.</summary>
public sealed record SkippedSession(string Name);