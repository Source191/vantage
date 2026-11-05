namespace Source191.Torni.Application.Detail;

/// <summary>Whether a branch is merged, and into what (origin's default branch, or HEAD when the repository has none).</summary>
public sealed record MergeState(string Into, bool Merged);