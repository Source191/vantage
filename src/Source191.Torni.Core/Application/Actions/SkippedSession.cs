using Source191.Torni.Domain;

namespace Source191.Torni.Application.Actions;

/// <summary>A session close-empty left alone because it has messages by the time it runs.</summary>
public sealed record SkippedSession(ListedName Name);