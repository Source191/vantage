using Source191.Vantage.Application;
using Source191.Vantage.Application.TerminalSetup;

namespace Source191.Vantage.Core.Tests.Application;

/// <summary>
/// The command line as the user meets it: the root command invoked with in-memory writers and actions that only
/// record what reached them. Help, version and parse errors are System.CommandLine's, so these facts check what they
/// name and their exit codes, not their exact layout.
/// </summary>
public sealed class CommandLineShould : IDisposable
{
    private readonly List<string> calls = [];
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();

    private Task<int> InvokeAsync(params string[] arguments)
    {
        CommandActions actions = new(
            OpenConsole: _ => Record("console"),
            StartHere: (name, prompt, _) => Record($"here name={name ?? "(none)"} prompt={prompt ?? "(none)"}"),
            SetUpTerminal: (request, _) => Record($"setup {request}"),
            ClaimTab: (registration, processId, _) => Record($"claim {registration:N} {processId}"));

        return CommandLine.InvokeAsync(CommandLine.Build(actions), arguments, output, error);
    }

    private Task<int> Record(string call)
    {
        calls.Add(call);

        return Task.FromResult(0);
    }

    [Fact]
    public async Task OpenTheConsoleWithNoArguments()
    {
        int exitCode = await InvokeAsync();

        exitCode.ShouldBe(0);
        calls.ShouldBe(["console"]);
    }

    [Theory]
    [InlineData(new[] { "here", "fix-tests", "-p", "Fix them" }, "here name=fix-tests prompt=Fix them")]
    [InlineData(new[] { "here" }, "here name=(none) prompt=(none)")]
    [InlineData(new[] { "here", "fix-tests", "--prompt=Fix them" }, "here name=fix-tests prompt=Fix them")]
    [InlineData(new[] { "here", "-p:Fix them" }, "here name=(none) prompt=Fix them")]
    [InlineData(new[] { "here", "--", "-experiment" }, "here name=-experiment prompt=(none)")]
    [InlineData(new[] { "here", "help" }, "here name=help prompt=(none)")]
    public async Task StartHereWithTheNameAndPromptGiven(string[] arguments, string call)
    {
        int exitCode = await InvokeAsync(arguments);

        exitCode.ShouldBe(0);
        calls.ShouldBe([call]);
    }

    [Theory]
    [InlineData("--promt")]
    [InlineData("-x")]
    public async Task RefuseANameThatLooksLikeAnOption(string token)
    {
        int exitCode = await InvokeAsync("here", token);

        exitCode.ShouldBe(1);
        calls.ShouldBeEmpty();
        error.ToString().ShouldContain(Wording.NameLooksLikeAnOption(token));
        output.ToString().ShouldContain("--prompt");
    }

    [Fact]
    public async Task TakeAPromptThatStartsWithADash()
    {
        int exitCode = await InvokeAsync("here", "fix-tests", "-p", "-fix the tests");

        exitCode.ShouldBe(0);
        calls.ShouldBe(["here name=fix-tests prompt=-fix the tests"]);
    }

    [Fact]
    public async Task SetUpTheTerminalFromItsOptions()
    {
        int exitCode = await InvokeAsync("setup-terminal", "--sound", @"C:\Windows\Media\chimes.wav", "--yes");

        exitCode.ShouldBe(0);
        calls.ShouldBe([$"setup {new TerminalSetupRequest(@"C:\Windows\Media\chimes.wav", SessionSound: null, Yes: true, Remove: false)}"]);
    }

    [Fact]
    public async Task ClaimATabWithItsRegistrationAndProcess()
    {
        Guid registration = Guid.NewGuid();

        int exitCode = await InvokeAsync("claim-tab", registration.ToString("N"), "4242");

        exitCode.ShouldBe(0);
        calls.ShouldBe([$"claim {registration:N} 4242"]);
    }

    [Fact]
    public async Task PassOnTheActionsExitCode()
    {
        CommandActions actions = new(_ => Task.FromResult(0), (_, _, _) => Task.FromResult(2), (_, _) => Task.FromResult(0), (_, _, _) => Task.FromResult(0));

        int exitCode = await CommandLine.InvokeAsync(CommandLine.Build(actions), ["here", "a;b"], output, error);

        exitCode.ShouldBe(2);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/h")]
    [InlineData("-?")]
    [InlineData("/?")]
    public async Task PrintTheHelpForEverySpelling(string flag)
    {
        int exitCode = await InvokeAsync(flag);

        exitCode.ShouldBe(0);
        calls.ShouldBeEmpty();
        output.ToString().ShouldContain("here");
        output.ToString().ShouldContain("setup-terminal");
        output.ToString().ShouldContain("Claude Code");
        output.ToString().ShouldNotContain("claim-tab");
    }

    [Fact]
    public async Task DescribeVantageAsMissionControl()
    {
        await InvokeAsync("--help");

        output.ToString().ShouldContain("Mission control for Claude Code sessions: every session in one tab, still there after a reboot.");
    }

    [Fact]
    public async Task SayWhereToSupportVantageInTheHelp()
    {
        await InvokeAsync("--help");

        output.ToString().ShouldContain(Wording.SupportLine);
    }

    [Theory]
    [InlineData("here", "--help")]
    [InlineData("here", "fix-tests", "-h")]
    public async Task PrintTheHelpOfVantageHere(params string[] arguments)
    {
        int exitCode = await InvokeAsync(arguments);

        exitCode.ShouldBe(0);
        calls.ShouldBeEmpty();
        output.ToString().ShouldContain("--prompt");
        output.ToString().ShouldContain("name");
    }

    [Fact]
    public async Task PrintTheHelpOfSetupTerminal()
    {
        int exitCode = await InvokeAsync("setup-terminal", "-h");

        exitCode.ShouldBe(0);
        calls.ShouldBeEmpty();
        foreach (string option in new[] { "--sound", "--session-sound", "--yes", "--remove" })
        {
            output.ToString().ShouldContain(option);
        }
    }

    [Fact]
    public async Task PrintTheVersion()
    {
        int exitCode = await InvokeAsync("--version");

        exitCode.ShouldBe(0);
        calls.ShouldBeEmpty();
        output.ToString().ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("frob")]
    [InlineData("--frob")]
    [InlineData("?")]
    [InlineData("-v")]
    [InlineData("help")]
    [InlineData("here", "-p", "first", "-p", "second")]
    [InlineData("here", "fix-tests", "-d", "Flaky orders suite")]
    [InlineData("claim-tab", "not-a-guid", "4242")]
    public async Task RefuseWhatItCannotParseWithExitOne(params string[] arguments)
    {
        int exitCode = await InvokeAsync(arguments);

        exitCode.ShouldBe(1);
        calls.ShouldBeEmpty();
        error.ToString().ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("hre", "here", new[] { "hre" })]
    [InlineData("--sund", "--sound", new[] { "setup-terminal", "--sund", @"C:\Windows\Media\chimes.wav" })]
    public async Task SuggestACloseMatch(string mistyped, string suggestion, string[] arguments)
    {
        int exitCode = await InvokeAsync(arguments);

        exitCode.ShouldBe(1);
        calls.ShouldBeEmpty();
        error.ToString().ShouldContain(mistyped);
        output.ToString().ShouldContain(suggestion);
    }

    public void Dispose()
    {
        output.Dispose();
        error.Dispose();
    }
}