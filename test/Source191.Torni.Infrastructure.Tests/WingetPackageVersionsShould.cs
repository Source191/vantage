using Source191.Torni.Adapters.Packages;
using Source191.Torni.Domain;
using Source191.Torni.Ports;
using System.Net;
using System.Text;

namespace Source191.Torni.Infrastructure.Tests;

/// <summary>The winget community repository and GitHub adapter over a stub handler: no request ever leaves the machine.</summary>
public sealed class WingetPackageVersionsShould
{
    private const string Folders = """
        [
          { "name": "0.20.0", "type": "dir" },
          { "name": "0.21.0", "type": "dir" },
          { "name": "0.22.0-preview.1", "type": "dir" },
          { "name": "README.md", "type": "file" },
          { "name": "notes", "type": "dir" }
        ]
        """;

    private static readonly ToolVersion Version = new("0.9.0");

    [Fact]
    public async Task ListTheStableVersionFoldersOfThePackagesManifests()
    {
        StubHandler handler = new(_ => Json(Folders));
        using WingetPackageVersions packages = new(Version, handler);

        VersionListing listing = await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken);

        listing.ShouldBeOfType<VersionListing.Listed>().Versions.ShouldBe(["0.20.0", "0.21.0"]);
        HttpRequestMessage request = handler.Requests.Single();
        request.RequestUri.ShouldBe(new Uri("https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests/s/Source191/Torni"));
        request.Headers.UserAgent.ToString().ShouldBe("Torni/0.9.0");
        request.Headers.Accept.ToString().ShouldBe("application/vnd.github+json");
    }

    [Fact]
    public async Task ListNothingForAPackageWithNoVersionsYet()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => Json("[]")));

        VersionListing listing = await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken);

        listing.ShouldBeOfType<VersionListing.Listed>().Versions.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenThePackageIsNotInTheRepository()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        (await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenRateLimited()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));

        (await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("null")]
    [InlineData("{ \"name\": \"0.21.0\", \"type\": \"dir\" }")]
    [InlineData("[ { \"type\": \"dir\" } ]")]
    public async Task AnswerNothingLearnedForAnUnreadableListing(string body)
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => Json(body)));

        (await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenTheRequestFails()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => throw new HttpRequestException("no network")));

        (await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task AnswerNothingLearnedWhenTheRequestTimesOut()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => throw new TaskCanceledException("timed out", new TimeoutException())));

        (await packages.ListAsync("Source191.Torni", TestContext.Current.CancellationToken)).ShouldBe(new VersionListing.NothingLearned());
    }

    [Fact]
    public async Task ReadTheNotesFromTheChangelogAtTheReleasesTag()
    {
        StubHandler handler = new(_ => Text("## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)\n"));
        using WingetPackageVersions packages = new(Version, handler);

        PackageNotes notes = await packages.NotesAsync("Source191.Torni", ReleaseVersion("0.21.0"), TestContext.Current.CancellationToken);

        notes.ShouldBe(new PackageNotes.Read("## 0.21.0\n### Added\n- A key that opens the Northwind order (#171)\n"));
        HttpRequestMessage request = handler.Requests.Single();
        request.RequestUri.ShouldBe(new Uri("https://raw.githubusercontent.com/Source191/torni/v0.21.0/CHANGELOG.md"));
        request.Headers.UserAgent.ToString().ShouldBe("Torni/0.9.0");
    }

    [Fact]
    public async Task LearnNothingFromAMissingChangelog()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        (await packages.NotesAsync("Source191.Torni", ReleaseVersion("0.21.0"), TestContext.Current.CancellationToken)).ShouldBe(new PackageNotes.NothingLearned());
    }

    [Fact]
    public async Task LearnNothingWhenTheNotesRequestTimesOut()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => throw new TaskCanceledException("timed out", new TimeoutException())));

        (await packages.NotesAsync("Source191.Torni", ReleaseVersion("0.21.0"), TestContext.Current.CancellationToken)).ShouldBe(new PackageNotes.NothingLearned());
    }

    [Fact]
    public async Task LearnNothingWhenTheNotesRequestFails()
    {
        using WingetPackageVersions packages = new(Version, new StubHandler(_ => throw new HttpRequestException("no network")));

        (await packages.NotesAsync("Source191.Torni", ReleaseVersion("0.21.0"), TestContext.Current.CancellationToken)).ShouldBe(new PackageNotes.NothingLearned());
    }

    [Fact]
    public void GiveUpAfterThreeSeconds()
    {
        WingetPackageVersions.Timeout.ShouldBe(TimeSpan.FromSeconds(3));
    }

    private static ReleaseVersion ReleaseVersion(string text)
    {
        Source191.Torni.Domain.ReleaseVersion.TryParse(text, out ReleaseVersion? version).ShouldBeTrue();

        return version;
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Text(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };

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