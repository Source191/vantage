namespace Source191.Vantage.Domain;

/// <summary>One line of a release's notes, under its heading, as written in the changelog with its pull request number and no control characters.</summary>
public sealed record ReleaseEntry(ChangeKind Kind, string Text);