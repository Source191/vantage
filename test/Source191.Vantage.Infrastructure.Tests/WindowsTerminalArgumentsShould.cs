using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Domain;
using Source191.Vantage.Ports;
using System.Text;

namespace Source191.Vantage.Infrastructure.Tests;

public sealed class WindowsTerminalArgumentsShould
{
    private static readonly TabLaunch Launch = new(
        Title: "Refund retry policy",
        Colour: TabPalette.Default[1],
        StartingDirectory: FolderPath.From(@"C:\repos\Northwind.Orders"),
        Script: "& 'C:\\Users\\me\\.local\\bin\\claude.exe' attach 81cdb0f9");

    private static readonly List<string> Arguments = [.. WindowsTerminalArguments.ForTab(Launch, profile: [])];

    [Fact]
    public void OpenANewTabInTheCurrentWindow()
    {
        Arguments.Take(3).ShouldBe(["-w", "0", "new-tab"]);
    }

    [Fact]
    public void PassTheTitleColourAndStartingDirectoryAndSuppressTheApplicationTitle()
    {
        Arguments.ShouldContain("--title");
        Arguments[Arguments.IndexOf("--title") + 1].ShouldBe("Refund retry policy");
        Arguments[Arguments.IndexOf("--tabColor") + 1].ShouldBe("#0078D4");
        Arguments[Arguments.IndexOf("--startingDirectory") + 1].ShouldBe(@"C:\repos\Northwind.Orders");
        Arguments.ShouldContain("--suppressApplicationTitle");
    }

    [Fact]
    public void RunPowerShellWithTheEncodedScriptAndKeepTheTabOpen()
    {
        int pwsh = Arguments.IndexOf("pwsh.exe");

        Arguments.Skip(pwsh).Take(3).ShouldBe(["pwsh.exe", "-NoExit", "-EncodedCommand"], customMessage: "the profile must load so the shell behind the session is the normal one");
        string decoded = Encoding.Unicode.GetString(Convert.FromBase64String(Arguments[pwsh + 3]));
        decoded.ShouldBe(Launch.Script);
    }

    [Fact]
    public void EscapeSemicolonsSoWindowsTerminalDoesNotSplitOnThem()
    {
        TabLaunch launch = Launch with { Title = "fix a;b", StartingDirectory = FolderPath.From(@"C:\repos\Northwind;Orders") };

        List<string> arguments = [.. WindowsTerminalArguments.ForTab(launch, profile: [])];

        arguments[arguments.IndexOf("--title") + 1].ShouldBe(@"fix a\;b");
        arguments[arguments.IndexOf("--startingDirectory") + 1].ShouldBe(@"C:\repos\Northwind\;Orders");
        Encoding.Unicode.GetString(Convert.FromBase64String(arguments[^1])).ShouldBe(launch.Script);
    }

    [Fact]
    public void AddTheProfileWhenGiven()
    {
        List<string> withProfile = [.. WindowsTerminalArguments.ForTab(Launch, ["--profile", "Vantage session"])];

        withProfile[withProfile.IndexOf("--profile") + 1].ShouldBe("Vantage session");
        withProfile.IndexOf("--profile").ShouldBeLessThan(withProfile.IndexOf("pwsh.exe"));
    }

    [Fact]
    public void OmitTheProfileWhenEmpty()
    {
        Arguments.ShouldNotContain("--profile");
    }

    [Fact]
    public void BuildTheWholeArgumentListWithAndWithoutTheProfile()
    {
        string[] tail = ["--title", "Refund retry policy", "--tabColor", "#0078D4", "--suppressApplicationTitle", "--startingDirectory", @"C:\repos\Northwind.Orders", "pwsh.exe", "-NoExit", "-EncodedCommand", EncodedCommand.Encode(Launch.Script)];

        Arguments.ShouldBe(["-w", "0", "new-tab", .. tail]);
        WindowsTerminalArguments.ForTab(Launch, ["--profile", "Vantage session"]).ShouldBe(["-w", "0", "new-tab", "--profile", "Vantage session", .. tail]);
    }

    [Fact]
    public void PutTheTabOptionsBeforeTheCommand()
    {
        Arguments.IndexOf("--suppressApplicationTitle").ShouldBeLessThan(Arguments.IndexOf("pwsh.exe"));
    }
}