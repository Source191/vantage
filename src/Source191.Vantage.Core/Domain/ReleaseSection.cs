namespace Source191.Vantage.Domain;

/// <summary>A released version's section of the changelog: its entries in the order written.</summary>
public sealed record ReleaseSection(ReleaseVersion Version, IReadOnlyList<ReleaseEntry> Entries);