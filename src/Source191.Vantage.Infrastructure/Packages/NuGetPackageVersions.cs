using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Source191.Vantage.Adapters.Packages;

/// <summary>
/// nuget.org's flat-container index for a package (<c>{"versions": [...]}</c>, every listed version, no key needed):
/// one anonymous request with a 3 second timeout and a <c>Vantage/&lt;version&gt;</c> user agent. Any failure answers null.
/// </summary>
public sealed class NuGetPackageVersions : IPackageVersions, IDisposable
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private static readonly Uri Index = new("https://api.nuget.org/v3-flatcontainer/");

    private readonly HttpClient client;

    public NuGetPackageVersions(ToolVersion version)
        : this(version, new HttpClientHandler())
    {
    }

    public NuGetPackageVersions(ToolVersion version, HttpMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(version);
        client = new HttpClient(handler) { BaseAddress = Index, Timeout = Timeout };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Vantage", version.Text));
    }

    public async Task<IReadOnlyList<string>?> ListAsync(string packageId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
        try
        {
            using HttpResponseMessage response = await client.GetAsync(new Uri($"{packageId.ToLowerInvariant()}/index.json", UriKind.Relative), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
            VersionIndex? index = await JsonSerializer.DeserializeAsync<VersionIndex>(body, JsonSerializerOptions.Web, cancellationToken);

            return index?.Versions;
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => client.Dispose();

    private sealed record VersionIndex(IReadOnlyList<string>? Versions);
}