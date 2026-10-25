namespace Source191.Vantage.Ports;

/// <summary>The versions a package registry lists for a package, for the update check.</summary>
public interface IPackageVersions
{
    /// <summary>
    /// Every listed version of <paramref name="packageId"/> as the registry writes it, or null when the request fails,
    /// times out or answers something unreadable: the caller treats null as "nothing learned" and never shows why.
    /// </summary>
    Task<IReadOnlyList<string>?> ListAsync(string packageId, CancellationToken cancellationToken);
}
