using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Globalization;
using System.Text.Json;

namespace Source191.Vantage.Infrastructure.Tests.Contract;

/// <summary>
/// Vantage's own Claude adapters against the installed CLI: what fails here is what Vantage would get wrong. Each
/// session is started with no prompt, so no model call is made, in this repository (which the nightly workflow
/// trusts), and is stopped and removed by job id, since stop and rm take only ids.
/// </summary>
[Trait("Category", ClaudeContract.Category)]
public sealed class ClaudeContractShould
{
    private static readonly TimeSpan ListingDeadline = TimeSpan.FromSeconds(30);

    /// <summary>The fields an idle background row carries and the listing's parser reads.</summary>
    private static readonly string[] FieldsVantageReads = ["pid", "id", "cwd", "kind", "startedAt", "sessionId", "name", "status"];

    private readonly ProcessRunner runner = new();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReadTheVersion()
    {
        ClaudeCli cli = new(runner, ClaudeContract.Executable());

        string? version = await cli.VersionAsync(Cancellation);

        ReleaseVersion.TryParse(version).ShouldNotBeNull($"claude --version gave \"{version}\"");
    }

    [Fact]
    public async Task ListAStartedBackgroundSessionInAStateVantageKnows()
    {
        ClaudeCli cli = new(runner, ClaudeContract.Executable());
        SessionName name = UniqueName("listed");

        string jobId = await StartAsync(cli, name);
        try
        {
            Session session = await ListedAsync(cli, name);

            BackgroundSession background = session.ShouldBeOfType<BackgroundSession>();
            background.JobId.Value.ShouldBe(jobId);
            background.State.Kind.ShouldNotBe(DisplayStateKind.Unknown, $"the CLI reported a status or state Vantage does not know: {background.State.Label}");
        }
        finally
        {
            await EndAsync(cli, jobId);
        }
    }

    [Fact]
    public async Task GiveTheStartedRowEveryFieldVantageReads()
    {
        string executable = ClaudeContract.Executable();
        ClaudeCli cli = new(runner, executable);
        SessionName name = UniqueName("fields");

        string jobId = await StartAsync(cli, name);
        try
        {
            await ListedAsync(cli, name);
            ProcessResult listing = await runner.RunAsync(new ProcessRequest(executable, ["agents", "--json", "--all"], WorkingDirectory: null), Cancellation);
            using JsonDocument rows = JsonDocument.Parse(listing.StandardOutput);

            JsonElement row = rows.RootElement.EnumerateArray().Single(element => element.TryGetProperty("name", out JsonElement value) && value.GetString() == name.Value);
            string[] present = [.. row.EnumerateObject().Select(property => property.Name)];

            FieldsVantageReads.Except(present).ShouldBeEmpty($"the row has [{string.Join(", ", present)}]");
        }
        finally
        {
            await EndAsync(cli, jobId);
        }
    }

    [Fact]
    public async Task StartWithAnEightCharacterJobIdAndTheMiddleDotDecodedCorrectly()
    {
        ClaudeCli cli = new(runner, ClaudeContract.Executable());

        StartResult started = await cli.StartBackgroundAsync(RepositoryFolder.Root(), UniqueName("encoding"), openingPrompt: null, Cancellation);
        try
        {
            started.Outcome.ShouldBe(StartOutcome.Started, started.Message);
            started.JobId.ShouldNotBeNull();
            started.JobId.Length.ShouldBe(8);
            started.Message.ShouldContain("·");
        }
        finally
        {
            if (started.JobId is not null)
            {
                await EndAsync(cli, started.JobId);
            }
        }
    }

    [Fact]
    public async Task RemoveTheSessionByItsJobId()
    {
        ClaudeCli cli = new(runner, ClaudeContract.Executable());
        SessionName name = UniqueName("removed");
        string jobId = await StartAsync(cli, name);
        try
        {
            await ListedAsync(cli, name);

            await EndAsync(cli, jobId);

            IReadOnlyList<Session> sessions = await cli.ListSessionsAsync(Cancellation);
            sessions.ShouldNotContain(session => session.Name == name.Value);
        }
        finally
        {
            // A second stop and rm of an id already gone is harmless: their results are not read.
            await EndAsync(cli, jobId);
        }
    }

    private static SessionName UniqueName(string purpose)
    {
        string stamp = TimeProvider.System.GetUtcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);

        return SessionName.From($"contract-{purpose}-{stamp}");
    }

    private static async Task<string> StartAsync(ClaudeCli cli, SessionName name)
    {
        StartResult started = await cli.StartBackgroundAsync(RepositoryFolder.Root(), name, openingPrompt: null, Cancellation);
        started.Outcome.ShouldBe(StartOutcome.Started, started.Message);

        return started.JobId.ShouldNotBeNull(started.Message);
    }

    /// <summary>The started session as the listing shows it; the supervisor can take a few seconds to list a new job.</summary>
    private static async Task<Session> ListedAsync(ClaudeCli cli, SessionName name)
    {
        DateTimeOffset deadline = TimeProvider.System.GetUtcNow() + ListingDeadline;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            IReadOnlyList<Session> sessions = await cli.ListSessionsAsync(Cancellation);
            Session? listed = sessions.FirstOrDefault(session => session.Name == name.Value);
            if (listed is not null)
            {
                return listed;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), Cancellation);
        }

        throw new ShouldAssertException($"{name.Value} was not listed by claude agents --json --all within {ListingDeadline.TotalSeconds} s.");
    }

    private static async Task EndAsync(ClaudeCli cli, string jobId)
    {
        await cli.StopAsync(jobId, Cancellation);
        await cli.RemoveAsync(jobId, Cancellation);
    }
}