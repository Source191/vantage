namespace Source191.Vantage.Ports;

/// <summary>The release notes shipped inside the installed tool, read without a network request.</summary>
public interface IPackagedReleaseNotes
{
    /// <summary>The changelog's text as packed, or <see cref="PackageNotes.NothingLearned"/> when it is missing or cannot be read.</summary>
    Task<PackageNotes> ReadAsync();
}