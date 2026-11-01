using Source191.Vantage.Domain;

namespace Source191.Vantage.Ports;

/// <summary>The versions a package registry lists for a package, and one version's release notes, for the update check.</summary>
public interface IPackageVersions
{
    /// <summary>
    /// Every listed version of <paramref name="packageId"/> as the registry writes it, or <see cref="VersionListing.NothingLearned"/>
    /// when the request fails, times out or answers something unreadable; the caller never shows why.
    /// </summary>
    Task<VersionListing> ListAsync(string packageId, CancellationToken cancellationToken);

    /// <summary>
    /// The release notes the registry holds for one version of <paramref name="packageId"/>, as text, or
    /// <see cref="PackageNotes.NothingLearned"/> when the request fails, times out or answers something unreadable.
    /// </summary>
    Task<PackageNotes> NotesAsync(string packageId, ReleaseVersion version, CancellationToken cancellationToken);
}