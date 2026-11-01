using Source191.Vantage.Adapters.Packages;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Net;
using System.Text;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The nuget.org adapter over a stub handler: no request ever leaves the machine.</summary>
public sealed class NuGetPackageVersionsShould
{
    private static readonly ToolVersion Version = new("0.9.0");

    [Fact]
    public async Task ListTheVersionsFromTheIndex()
    {
        StubHandler handler = new(_ => Json("""{ "versions": ["0.8.0", "0.9.0", "0.10.0"] }"""));
        using NuGetPackageVersions packages = new(Version, handler);

        VersionListing listing = await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken);

        listing.ShouldBeOfType<VersionListing.Listed>().Versions.ShouldBe(["0.8.0", "0.9.0", "0.10.0"]);
        HttpRequestMessage request = handler.Requests.Single();
        request.RequestUri.ShouldBe(new Uri("https://api.nuget.org/v3-flatcontainer/source191.vantage/index.json"));
        request.Headers.UserAgent.ToString().ShouldBe("Vantage/0.9.0");
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenThePackageIsNotFound()
    {
        using NuGetPackageVersions packages = new(Version, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        (await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task AnswerNothingLearnedOnMalformedJson()
    {
        using NuGetPackageVersions packages = new(Version, new StubHandler(_ => Json("{ not json")));

        (await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenTheRequestFails()
    {
        using NuGetPackageVersions packages = new(Version, new StubHandler(_ => throw new HttpRequestException("no network")));

        (await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenTheRequestTimesOut()
    {
        using NuGetPackageVersions packages = new(Version, new StubHandler(_ => throw new TaskCanceledException("timed out", new TimeoutException())));

        (await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task ListAnIndexWithNoVersionsAsAnEmptyListing()
    {
        using NuGetPackageVersions packages = new(Version, new StubHandler(_ => Json("""{ "versions": [] }""")));

        VersionListing listing = await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken);

        listing.ShouldBeOfType<VersionListing.Listed>().Versions.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{ \"versions\": null }")]
    [InlineData("null")]
    public async Task AnswerNothingLearnedForAReplyWithoutVersions(string body)
    {
        using NuGetPackageVersions packages = new(Version, new StubHandler(_ => Json(body)));

        (await packages.ListAsync("Source191.Vantage", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public void RefuseANullVersion()
    {
        using StubHandler handler = new(_ => Json("{}"));

        ArgumentNullException refused = Should.Throw<ArgumentNullException>(() => new NuGetPackageVersions(null!, handler));

        refused.ParamName.ShouldBe("version");
    }

    [Fact]
    public void GiveUpAfterThreeSeconds()
    {
        NuGetPackageVersions.Timeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            return Task.FromResult(answer(request));
        }
    }
}