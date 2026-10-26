namespace Source191.Vantage.Infrastructure.Tests.Contract;

/// <summary>Excerpts captured from "claude --help" and "claude agents --help" on 2026-10-26 (Claude Code 2.1.310).</summary>
public sealed class HelpSurfaceShould
{
    [Fact]
    public void ExtractTheCommandNames()
    {
        const string help = """
            Usage: claude [options] [command] [prompt]

            Commands:
              attach <id|name>                      Open a background session in this
                                                    terminal. <id> is the short id that
                                                    `claude --bg` prints and `claude agents`
                                                    lists. Part of the session name works
                                                    too
              rm <id>                               Delete a background session, and its
                                                    worktree when that is safe. Works on
                                                    sessions that have already exited
              stop|kill <id>                        Stop a background session. Its
                                                    conversation is kept: `claude attach
                                                    <id>` opens it again, `claude --resume`
                                                    works once it is stopped
            """;

        HelpSurface.Extract(help).ShouldBe(["attach", "kill", "rm", "stop"]);
    }

    [Fact]
    public void ExtractLongAndShortOptionsSortedAndDistinct()
    {
        const string help = """
            Options:
              --allowedTools, --allowed-tools <tools...>
                  Comma or space-separated list of tool names to allow (e.g. "Bash(git *)
                  Edit")
              --bg, --background                    Start the session in the background and
                                                    return immediately.
              -h, --help                            Display help for command
              -n, --name <name>                     Set a display name for this session
              -h, --help                            Display help for command
            """;

        HelpSurface.Extract(help).ShouldBe(["--allowed-tools", "--allowedTools", "--background", "--bg", "--help", "--name", "-h", "-n"]);
    }

    [Fact]
    public void IgnoreDescriptionsAndUsageProse()
    {
        const string before = """
            Usage: claude [options] [command] [prompt]

            Claude Code - starts an interactive session by default, use -p/--print for
            non-interactive output

            Arguments:
              prompt                                Your prompt

            Options:
              --bare                                Minimal mode: skip hooks. Explicitly provide context
                                                    via: --system-prompt[-file],
                                                    --append-system-prompt[-file], --add-dir
              -v, --version                         Output the version number
            """;
        const string after = """
            Usage: claude [options] [command] [prompt]

            Claude Code - starts an interactive session unless -p/--print is given

            Arguments:
              prompt                                The prompt to start with

            Options:
              --bare                                Minimal mode: skips hooks; give context with --system-prompt or --add-dir
              -v, --version                         Print the version number
            """;

        HelpSurface.Extract(before).ShouldBe(["--bare", "--version", "-v"]);
        HelpSurface.Extract(after).ShouldBe(HelpSurface.Extract(before));
    }
}