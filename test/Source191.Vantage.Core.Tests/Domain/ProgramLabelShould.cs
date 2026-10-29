using Source191.Vantage.Domain;

namespace Source191.Vantage.Core.Tests.Domain;

/// <summary>The command lines are the real ones a session's direct children ran on 29 October 2026, with the one secret redacted.</summary>
public sealed class ProgramLabelShould
{
    [Theory]
    [InlineData("cmd", "cmd /c npx -y @azure-devops/mcp m-kopa", "@azure-devops/mcp")]
    [InlineData("cmd", """C:\windows\system32\cmd.exe /d /s /c "npx ^"@playwright/mcp@latest^"" """, "@playwright/mcp")]
    [InlineData("cmd", """C:\windows\system32\cmd.exe /d /s /c "npx ^"@playwright/mcp@latest^" ^"--extension^"" """, "@playwright/mcp")]
    [InlineData("cmd", """C:\windows\system32\cmd.exe /d /s /c "npx ^"-y^" ^"@azure/mcp@latest^" ^"server^" ^"start^"" """, "@azure/mcp")]
    [InlineData("cmd", """C:\windows\system32\cmd.exe /d /s /c "npx ^"-y^" ^"@upstash/context7-mcp^" ^"--api-key^" ^"ctx7sk-example-secret^"" """, "@upstash/context7-mcp")]
    [InlineData("uvx", "uvx mcp-grafana@latest", "mcp-grafana")]
    public void NameThePackageAPackageRunnerStarts(string name, string commandLine, string expected)
    {
        ProgramLabel.From(name, commandLine).ShouldBe(expected);
    }

    [Fact]
    public void NameTheProgramACommandShellWraps()
    {
        const string chcpThenPwsh = """C:\windows\System32\cmd.exe /d /s /c ""C:\windows\System32\chcp.com" 65001 >nul & "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -NonInteractive -File C:\Users\me\AppData\Local\Temp\build.ps1" """;

        ProgramLabel.From("cmd", chcpThenPwsh).ShouldBe("pwsh");
        ProgramLabel.From("cmd", "cmd /c dir").ShouldBe("cmd");
    }

    [Fact]
    public void NameAnyOtherProgramByItsExecutable()
    {
        ProgramLabel.From("gk-alpha", @"C:\Users\me\AppData\Local\GitKrakenCLI\gk-alpha.exe mcp --host=claude-cli --session=kepler").ShouldBe("gk-alpha");
        ProgramLabel.From("node_repl", @"C:\Users\me\AppData\Local\OpenAI\Codex\runtimes\cua_node\1b23c930bdf84ed6\bin\node_repl.exe").ShouldBe("node_repl");
        ProgramLabel.From("Wispr Flow", string.Empty).ShouldBe("Wispr Flow");
    }

    [Fact]
    public void NeverCarryASecretFromTheCommandLine()
    {
        const string withKey = """C:\windows\system32\cmd.exe /d /s /c "npx ^"-y^" ^"@upstash/context7-mcp^" ^"--api-key^" ^"ctx7sk-example-secret^"" """;

        ProgramLabel.From("cmd", withKey).ShouldNotContain("ctx7sk");
    }
}
