using Source191.Vantage.Adapters.Claude;

namespace Source191.Vantage.Infrastructure.Tests.Contract;

/// <summary>
/// The gate for the tests that check the installed Claude Code CLI still offers what Vantage relies on. They start
/// real background sessions, so they run only when <see cref="Variable"/> is 1 (the nightly claude-contract
/// workflow sets it) and the CLI is installed; an ordinary test run skips them.
/// </summary>
internal static class ClaudeContract
{
    public const string Category = "ClaudeContract";

    public const string Variable = "VANTAGE_CLAUDE_CONTRACT";

    public static string Executable()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable(Variable) == "1", $"Set {Variable}=1 to check the installed Claude Code CLI.");
        string? executable = ClaudeExecutable.Locate();
        Assert.SkipWhen(executable is null, "Claude Code is not installed on this machine.");

        return executable;
    }
}