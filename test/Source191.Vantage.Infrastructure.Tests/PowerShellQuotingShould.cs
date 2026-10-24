using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Application;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>
/// Hands quoted text to real PowerShell 7, encoded exactly as a tab's script is, so the quoting is checked against
/// PowerShell's own tokenizer rather than against a reading of its rules.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PowerShellQuotingShould
{
    /// <summary>Every single-quote character, a backtick, a variable and a quote followed by a command.</summary>
    private const string Text = "O'Neill \u2018a\u2019 \u201Ab\u201B `n $name x'; Remove-Item C:\\nowhere; '";

    [Fact]
    public async Task PrintQuotedTextBackUnchanged()
    {
        string script = $"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8\nWrite-Output {PowerShellScript.Quote(Text)}";
        ProcessRequest request = new("pwsh.exe", ["-NoProfile", "-NonInteractive", "-EncodedCommand", EncodedCommand.Encode(script)], WorkingDirectory: null);

        ProcessResult result = await new ProcessRunner().RunAsync(request, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardError.ShouldBeEmpty("nothing in the text may run as a command");
        result.StandardOutput.TrimEnd('\r', '\n').ShouldBe(Text);
    }
}