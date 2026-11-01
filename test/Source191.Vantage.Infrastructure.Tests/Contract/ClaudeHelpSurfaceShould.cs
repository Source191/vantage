using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests.Contract;

/// <summary>
/// The commands and options of the installed CLI's help, against the committed snapshots in Contract\Snapshots. A
/// difference writes the new surface to &lt;name&gt;.received.txt beside the snapshot in the output folder; accept it by
/// copying that file over the snapshot, in a change that re-checks the CLI facts in .claude\rules\claude-code.md. Never add
/// "--bg --resume --help": it starts a session.
/// </summary>
[Trait("Category", ClaudeContract.Category)]
public sealed class ClaudeHelpSurfaceShould
{
    private static readonly string Snapshots = Path.Combine(AppContext.BaseDirectory, "Contract", "Snapshots");

    [Theory]
    [InlineData("claude-help.txt", "--help")]
    [InlineData("claude-agents-help.txt", "agents --help")]
    public async Task MatchTheSnapshot(string snapshot, string arguments)
    {
        string executable = ClaudeContract.Executable();

        ProcessResult help = await new ProcessRunner().RunAsync(new ProcessRequest(executable, arguments.Split(' '), WorkingFolder.Inherit), TestContext.Current.CancellationToken);
        IReadOnlyList<string> received = HelpSurface.Extract(help.StandardOutput);
        IReadOnlyList<string> expected = Lines(await File.ReadAllTextAsync(Path.Combine(Snapshots, snapshot), TestContext.Current.CancellationToken));

        string[] added = [.. received.Except(expected)];
        string[] removed = [.. expected.Except(received)];
        if (added.Length > 0 || removed.Length > 0)
        {
            await File.WriteAllTextAsync(Path.Combine(Snapshots, Path.ChangeExtension(snapshot, ".received.txt")), string.Join('\n', received) + "\n", TestContext.Current.CancellationToken);
        }

        added.ShouldBeEmpty($"claude {arguments} offers what {snapshot} does not; removed: [{string.Join(", ", removed)}]");
        removed.ShouldBeEmpty($"claude {arguments} no longer offers what {snapshot} lists");
    }

    private static IReadOnlyList<string> Lines(string text)
    {
        return [.. text.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0)];
    }
}