using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml;

namespace Source191.Vantage.Adapters.Packages;

/// <summary>
/// nuget.org's <see href="https://learn.microsoft.com/nuget/api/package-base-address-resource">flat-container index</see>
/// for a package (<c>{"versions": [...]}</c>, every listed version, no key needed):
/// one anonymous request with a 3 second timeout and a <c>Vantage/&lt;version&gt;</c> user agent. Any failure, or a reply without its versions, answers <see cref="VersionListing.NothingLearned"/>.
/// </summary>
public sealed class NuGetPackageVersions(ToolVersion version, HttpMessageHandler handler) : IPackageVersions, IDisposable
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private static readonly Uri Index = new("https://api.nuget.org/v3-flatcontainer/");

    private readonly HttpClient client = ClientFor(version ?? throw new ArgumentNullException(nameof(version)), handler);

    public async Task<VersionListing> ListAsync(string packageId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        try
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri($"{packageId.ToLowerInvariant()}/index.json", UriKind.Relative), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new VersionListing.NothingLearned();
            }

            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
            VersionIndex? index = await JsonSerializer.DeserializeAsync<VersionIndex>(body, JsonSerializerOptions.Web, cancellationToken);

            return index?.Versions is IReadOnlyList<string> versions ? new VersionListing.Listed(versions) : new VersionListing.NothingLearned();
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

    /// <summary>
    /// The version's nuspec (<c>{id}/{version}/{id}.nuspec</c>, both in lower case as the flat container names them) and
    /// its <c>metadata/releaseNotes</c>, read with DTD processing prohibited and no resolver; anything else learns nothing.
    /// </summary>
    public async Task<PackageNotes> NotesAsync(string packageId, ReleaseVersion version, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        ArgumentNullException.ThrowIfNull(version);
        string id = packageId.ToLowerInvariant();
        try
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri($"{id}/{version.ToString().ToLowerInvariant()}/{id}.nuspec", UriKind.Relative), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new PackageNotes.NothingLearned();
            }

            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);

            return await ReleaseNotesAsync(body) is string notes ? new PackageNotes.Read(notes) : new PackageNotes.NothingLearned();
        }
        catch (HttpRequestException)
        {
            return new PackageNotes.NothingLearned();
        }
        catch (TaskCanceledException)
        {
            return new PackageNotes.NothingLearned();
        }
        catch (XmlException)
        {
            return new PackageNotes.NothingLearned();
        }
    }

    public void Dispose() => client.Dispose();

    /// <summary>The text of <c>package/metadata/releaseNotes</c>, matched by local name because the nuspec's namespace changes with its schema version; null when there is none.</summary>
    private static async Task<string?> ReleaseNotesAsync(Stream body)
    {
        XmlReaderSettings settings = new() { Async = true, DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreComments = true };
        using XmlReader reader = XmlReader.Create(body, settings);
        string[] path = ["package", "metadata", "releaseNotes"];
        bool more = await reader.ReadAsync();
        while (more)
        {
            bool onPath = reader.NodeType == XmlNodeType.Element && reader.Depth < path.Length && reader.LocalName == path[reader.Depth];
            if (onPath && reader.Depth == path.Length - 1)
            {
                return await reader.ReadElementContentAsStringAsync();
            }

            if (reader.NodeType == XmlNodeType.Element && !onPath)
            {
                // Skip leaves the reader on the node after the element, which must be looked at before reading on.
                await reader.SkipAsync();
                more = !reader.EOF;
                continue;
            }

            more = await reader.ReadAsync();
        }
        return null;
    }

    private static HttpClient ClientFor(ToolVersion version, HttpMessageHandler handler)
    {
        HttpClient client = new(handler) { BaseAddress = Index, Timeout = Timeout };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Vantage", version.Text));

        return client;
    }

    private sealed record VersionIndex(IReadOnlyList<string>? Versions);
}