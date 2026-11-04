using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Source191.Torni.Adapters.Packages;

/// <summary>
/// The versions winget can install, read from the package's manifest folders in the
/// <see href="https://github.com/microsoft/winget-pkgs">winget community repository</see> through GitHub's
/// <see href="https://docs.github.com/rest/repos/contents">contents API</see> (a folder per version, made only when its
/// manifest's pull request is merged), and a release's notes from <c>CHANGELOG.md</c> at its tag in Torni's own
/// repository. Each is one anonymous request with a 3 second timeout and a <c>Torni/&lt;version&gt;</c> user agent,
/// which GitHub requires; any failure, a rate limit or an unreadable reply answers nothing learned.
/// </summary>
public sealed class WingetPackageVersions(ToolVersion version, HttpMessageHandler handler) : IPackageVersions, IDisposable
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private static readonly Uri Manifests = new("https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests/");
    private static readonly Uri Changelogs = new("https://raw.githubusercontent.com/Source191/torni/");

    private readonly HttpClient client = ClientFor(version, handler);

    /// <summary>The names of the package's version folders that read as stable versions; a prerelease or any other folder or file is left out.</summary>
    public async Task<VersionListing> ListAsync(string packageId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, new Uri(Manifests, ManifestFolder(packageId)));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new VersionListing.NothingLearned();
            }

            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
            IReadOnlyList<FolderEntry?>? entries = await JsonSerializer.DeserializeAsync<IReadOnlyList<FolderEntry?>>(body, JsonSerializerOptions.Web, cancellationToken);

            return Listing(entries);
        }
        catch (HttpRequestException)
        {
            return new VersionListing.NothingLearned();
        }
        catch (TaskCanceledException)
        {
            return new VersionListing.NothingLearned();
        }
        catch (JsonException)
        {
            return new VersionListing.NothingLearned();
        }
    }

    /// <summary>The changelog as it stood at the release's tag, <c>v&lt;version&gt;</c>, as text; anything else learns nothing.</summary>
    public async Task<PackageNotes> NotesAsync(string packageId, ReleaseVersion version, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        try
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri(Changelogs, $"v{version}/CHANGELOG.md"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new PackageNotes.NothingLearned();
            }

            return new PackageNotes.Read(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (HttpRequestException)
        {
            return new PackageNotes.NothingLearned();
        }
        catch (TaskCanceledException)
        {
            return new PackageNotes.NothingLearned();
        }
    }

    public void Dispose() => client.Dispose();

    /// <summary>A package's folder in the repository: its identifier's first letter in lower case, then each dotted part, as in <c>s/Source191/Torni</c>.</summary>
    private static string ManifestFolder(string packageId) => $"{char.ToLowerInvariant(packageId[0])}/{packageId.Replace('.', '/')}";

    private static VersionListing Listing(IReadOnlyList<FolderEntry?>? entries)
    {
        if (entries is null || !entries.All(entry => entry is { Name: not null, Type: not null }))
        {
            return new VersionListing.NothingLearned();
        }

        IReadOnlyList<string> versions = [.. entries
            .OfType<FolderEntry>()
            .Where(entry => string.Equals(entry.Type, "dir", StringComparison.Ordinal))
            .Select(entry => entry.Name ?? string.Empty)
            .Where(name => ReleaseVersion.TryParse(name, out ReleaseVersion? parsed) && !parsed.IsPrerelease)];

        return new VersionListing.Listed(versions);
    }

    private static HttpClient ClientFor(ToolVersion version, HttpMessageHandler handler)
    {
        HttpClient client = new(handler) { Timeout = Timeout };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Torni", version.Text));

        return client;
    }

    private sealed record FolderEntry(string? Name, string? Type);
}