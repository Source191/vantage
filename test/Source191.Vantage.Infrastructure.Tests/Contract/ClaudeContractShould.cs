using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

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

    private static readonly TimeSpan ExitDeadline = TimeSpan.FromSeconds(60);

    /// <summary>The fields an idle background row carries and the listing's parser reads.</summary>
    private static readonly string[] FieldsVantageReads = ["pid", "id", "cwd", "kind", "startedAt", "sessionId", "name", "status"];

    private readonly ProcessRunner runner = new();

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ReadTheVersion()
    {
        ClaudeCli cli = new(runner, ClaudeContract.Executable());

        string? version = await cli.VersionAsync(Cancellation);

        ReleaseVersion.TryParse(version, out _).ShouldBeTrue($"claude --version gave \"{version}\"");
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
            StartResult.Started running = started.ShouldBeOfType<StartResult.Started>(started.Message);
            running.JobId.Value.Length.ShouldBe(8);
            running.Message.ShouldContain("·");
        }
        finally
        {
            if (started is StartResult.Started { JobId: JobId jobId })
            {
                await EndAsync(cli, jobId.Value);
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

            IReadOnlyList<Session> sessions = (await cli.ListSessionsAsync(Cancellation)).Sessions;
            sessions.ShouldNotContain(session => session.Name == name.Value);
        }
        finally
        {
            // A second stop and rm of an id already gone is harmless: their results are not read.
            await EndAsync(cli, jobId);
        }
    }

    /// <summary>
    /// <c>claude -p</c> with no prompt waits 3 s for its standard input, fails on the missing input without calling the model, so the
    /// fact spends no tokens, and exits 1. On a clean machine it registers at about 7 s and exits at about 8.5 s, before a listing
    /// can see it, so <see cref="HoldOpenSettings"/> gives it a SessionEnd hook that sleeps for 20 s and keeps it registered and
    /// listed as an Agent SDK run is (interactive, no status). The exit is awaited for <see cref="ExitDeadline"/> after the listing,
    /// because the hook may run for its whole timeout of 40 s and a developer's own SessionEnd hooks lengthen the run further.
    /// </summary>
    [Fact]
    public async Task ListAPromptlessPrintRunAsAnAutomatedRun()
    {
        string executable = ClaudeContract.Executable();
        ClaudeCli cli = new(runner, executable);
        using Process print = StartPrintRun(executable);
        try
        {
            SessionListing listing = await ListedRunAsync(cli, print.Id);

            listing.Sessions.OfType<InteractiveSession>().Select(session => session.ProcessId).ShouldNotContain(print.Id, "the run is listed as a session too");
            print.StandardInput.Close();
            await print.WaitForExitAsync(Cancellation).WaitAsync(ExitDeadline, Cancellation);
            print.ExitCode.ShouldBe(1, "claude -p with no prompt and no input should fail before calling the model");
        }
        finally
        {
            if (!print.HasExited)
            {
                print.Kill(entireProcessTree: true);
            }
        }
    }

    private static Process StartPrintRun(string executable)
    {
        ProcessStartInfo start = new(executable)
        {
            WorkingDirectory = RepositoryFolder.Root(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("-p");
        start.ArgumentList.Add("--settings");
        start.ArgumentList.Add(HoldOpenSettings());
        foreach (string name in start.Environment.Keys.Where(InheritedEnvironment.ShouldDrop).ToList())
        {
            start.Environment.Remove(name);
        }

        return Process.Start(start) ?? throw new ShouldAssertException("claude -p did not start.");
    }

    /// <summary>
    /// The settings for <c>--settings</c>: a SessionEnd hook runs after the input has failed, so holding the run open there can never
    /// let it reach the model, and the timeout stops the CLI cutting the sleep short.
    /// </summary>
    private static string HoldOpenSettings()
    {
        JsonObject hook = new()
        {
            ["type"] = "command",
            ["command"] = "pwsh -NoProfile -Command Start-Sleep -Seconds 20",
            ["timeout"] = 40,
        };
        JsonObject settings = new()
        {
            ["hooks"] = new JsonObject
            {
                ["SessionEnd"] = new JsonArray(new JsonObject { ["hooks"] = new JsonArray(hook) }),
            },
        };

        return settings.ToJsonString();
    }

    /// <summary>The listing once it holds the run; a new process takes a few seconds to register.</summary>
    private static async Task<SessionListing> ListedRunAsync(ClaudeCli cli, int processId)
    {
        DateTimeOffset deadline = TimeProvider.System.GetUtcNow() + ListingDeadline;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            SessionListing listing = await cli.ListSessionsAsync(Cancellation);
            if (listing.AutomatedRuns.Any(run => run.ProcessId == processId))
            {
                return listing;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), Cancellation);
        }

        throw new ShouldAssertException($"claude -p (process {processId}) was not listed as an automated run within {ListingDeadline.TotalSeconds} s.");
    }

    private static SessionName UniqueName(string purpose)
    {
        string stamp = TimeProvider.System.GetUtcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);

        return SessionName.From($"contract-{purpose}-{stamp}");
    }

    private static async Task<string> StartAsync(ClaudeCli cli, SessionName name)
    {
        StartResult started = await cli.StartBackgroundAsync(RepositoryFolder.Root(), name, openingPrompt: null, Cancellation);
        return started.ShouldBeOfType<StartResult.Started>(started.Message).JobId.Value;
    }

    /// <summary>The started session as the listing shows it; the supervisor can take a few seconds to list a new job.</summary>
    private static async Task<Session> ListedAsync(ClaudeCli cli, SessionName name)
    {
        DateTimeOffset deadline = TimeProvider.System.GetUtcNow() + ListingDeadline;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            IReadOnlyList<Session> sessions = (await cli.ListSessionsAsync(Cancellation)).Sessions;
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