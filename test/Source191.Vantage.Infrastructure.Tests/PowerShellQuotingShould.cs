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

    /// <summary>The update tab's script is only parsed, never run: running it would update the installed tool.</summary>
    [Fact]
    public async Task ParseTheUpdateTabScriptWithoutErrors()
    {
        string update = new TabScripts(@"C:\Users\O'Neill\.local\bin\claude.exe", @"C:\Users\O'Neill\.dotnet\tools\vantage.exe").Update(4242, Domain.FolderPath.From(@"C:\Users\O'Neill")).Script;
        string script = $"$errors = $null\n[void][System.Management.Automation.Language.Parser]::ParseInput({PowerShellScript.Quote(update)}, [ref]$null, [ref]$errors)\nWrite-Output $errors.Count";
        ProcessRequest request = new("pwsh.exe", ["-NoProfile", "-NonInteractive", "-EncodedCommand", EncodedCommand.Encode(script)], WorkingDirectory: null);

        ProcessResult result = await new ProcessRunner().RunAsync(request, TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0, result.StandardError);
        result.StandardOutput.Trim().ShouldBe("0");
    }
}