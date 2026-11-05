using Source191.Torni.Adapters.Terminal;
using Source191.Torni.Ports;
using System.Text.Json.Nodes;

namespace Source191.Torni.Infrastructure.Tests;

/// <summary>The real file system behind ITerminalFragment, on a throwaway folder standing in for Windows Terminal's Fragments root.</summary>
[Trait("Category", "Integration")]
public sealed class TerminalFragmentShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "test-fragments-" + Guid.NewGuid().ToString("N"));
    private readonly TerminalFragment fragment;

    public TerminalFragmentShould()
    {
        fragment = new TerminalFragment(root, Path.Combine(root, "tool", "absent.ico"));
    }

    private static TerminalProfiles Profiles => new(@"C:\Users\me\.dotnet\tools\torni.exe", new BellSound.File(@"C:\Windows\Media\Windows Notify Messaging.wav"), new BellSound.File(@"C:\Windows\Media\Windows Proximity Notification.wav"));

    private JsonArray WrittenProfiles => JsonNode.Parse(File.ReadAllText(fragment.Location))!["profiles"]!.AsArray();

    [Fact]
    public async Task WriteBothProfilesAsUtf8WithoutABom()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);

        byte[] bytes = await File.ReadAllBytesAsync(fragment.Location, TestContext.Current.CancellationToken);
        bytes.Take(3).ShouldNotBe([(byte)0xEF, (byte)0xBB, (byte)0xBF]);
        fragment.Location.ShouldBe(Path.Combine(root, "Torni", "torni.json"));
        JsonArray profiles = WrittenProfiles;
        profiles.Count.ShouldBe(2);
        profiles[0]!["name"]!.GetValue<string>().ShouldBe("Torni");
        profiles[0]!["commandline"]!.GetValue<string>().ShouldBe("\"C:\\Users\\me\\.dotnet\\tools\\torni.exe\"");
        profiles[0]!["bellSound"]!.GetValue<string>().ShouldBe(@"C:\Windows\Media\Windows Notify Messaging.wav");
        profiles[1]!["name"]!.GetValue<string>().ShouldBe("Torni session");
        profiles[1]!["commandline"]!.GetValue<string>().ShouldBe("pwsh.exe -NoLogo");
        profiles[1]!["bellSound"]!.GetValue<string>().ShouldBe(@"C:\Windows\Media\Windows Proximity Notification.wav");
        foreach (JsonNode? profile in profiles)
        {
            profile!["bellStyle"]!.AsArray().Select(style => style!.GetValue<string>()).ShouldBe(["audible", "window", "taskbar"]);
        }
    }

    [Fact]
    public async Task LeaveOutTheSoundOfAProfileOnTheTerminalsOwnBell()
    {
        await fragment.WriteAsync(Profiles with { ConsoleSound = new BellSound.TerminalDefault() }, CancellationToken.None);

        JsonArray profiles = WrittenProfiles;
        profiles[0]!.AsObject().ContainsKey("bellSound").ShouldBeFalse();
        profiles[1]!.AsObject().ContainsKey("bellSound").ShouldBeTrue();
    }

    [Fact]
    public async Task WriteTheFragmentTextExactly()
    {
        await fragment.WriteAsync(Profiles with { SessionSound = new BellSound.TerminalDefault() }, CancellationToken.None);

        (await File.ReadAllTextAsync(fragment.Location, TestContext.Current.CancellationToken)).ShouldBe("""
            {
              "profiles": [
                {
                  "name": "Torni",
                  "commandline": "\"C:\\Users\\me\\.dotnet\\tools\\torni.exe\"",
                  "bellStyle": [
                    "audible",
                    "window",
                    "taskbar"
                  ],
                  "bellSound": "C:\\Windows\\Media\\Windows Notify Messaging.wav"
                },
                {
                  "name": "Torni session",
                  "commandline": "pwsh.exe -NoLogo",
                  "bellStyle": [
                    "audible",
                    "window",
                    "taskbar"
                  ]
                }
              ]
            }
            """.ReplaceLineEndings(Environment.NewLine));
    }

    [Fact]
    public async Task GiveTheConsoleProfileNoTabColourAndNoFixedTitle()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);

        JsonObject consoleProfile = WrittenProfiles[0]!.AsObject();
        consoleProfile.ContainsKey("tabColor").ShouldBeFalse();
        consoleProfile.ContainsKey("suppressApplicationTitle").ShouldBeFalse();
    }

    [Fact]
    public async Task ReplaceAnExistingFragment()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);

        await fragment.WriteAsync(Profiles with { ConsoleSound = new BellSound.File(@"C:\Windows\Media\chimes.wav"), SessionSound = new BellSound.TerminalDefault() }, CancellationToken.None);

        JsonArray profiles = WrittenProfiles;
        profiles.Count.ShouldBe(2);
        profiles[0]!["bellSound"]!.GetValue<string>().ShouldBe(@"C:\Windows\Media\chimes.wav");
        profiles[1]!.AsObject().ContainsKey("bellSound").ShouldBeFalse();
    }

    [Fact]
    public async Task ReportExistsOnlyWhenTheFileIsThere()
    {
        fragment.Exists.ShouldBeFalse();

        await fragment.WriteAsync(Profiles, CancellationToken.None);

        fragment.Exists.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveOnlyItsOwnFolder()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);
        string other = Path.Combine(root, "Other", "other.json");
        Directory.CreateDirectory(Path.GetDirectoryName(other)!);
        await File.WriteAllTextAsync(other, "{}", TestContext.Current.CancellationToken);

        bool removed = await fragment.RemoveAsync(CancellationToken.None);

        removed.ShouldBeTrue();
        Directory.Exists(Path.Combine(root, "Torni")).ShouldBeFalse();
        File.Exists(other).ShouldBeTrue();
        fragment.Exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ReportNothingToRemove()
    {
        bool removed = await fragment.RemoveAsync(CancellationToken.None);

        removed.ShouldBeFalse();
    }

    [Fact]
    public async Task GiveBothProfilesTheCopiedIcon()
    {
        string source = ToolIcon([1, 2, 3]);
        TerminalFragment withIcon = new(root, source);

        await withIcon.WriteAsync(Profiles, CancellationToken.None);

        string copy = Path.Combine(root, "Torni", "logo.ico");
        (await File.ReadAllBytesAsync(copy, TestContext.Current.CancellationToken)).ShouldBe([1, 2, 3]);
        foreach (JsonNode? profile in WrittenProfiles)
        {
            profile!["icon"]!.GetValue<string>().ShouldBe(copy);
        }
    }

    [Theory]
    [InlineData("missing.ico")]
    [InlineData(@"no-such-folder\logo.ico")]
    public async Task WriteNoIconWhenTheToolHasNone(string source)
    {
        TerminalFragment withoutIcon = new(root, Path.Combine(root, source));

        await withoutIcon.WriteAsync(Profiles, CancellationToken.None);

        foreach (JsonNode? profile in WrittenProfiles)
        {
            profile!.AsObject().ContainsKey("icon").ShouldBeFalse();
        }

        File.Exists(Path.Combine(root, "Torni", "logo.ico")).ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveTheIconWithTheFragment()
    {
        string source = ToolIcon([1]);
        TerminalFragment withIcon = new(root, source);
        await withIcon.WriteAsync(Profiles, CancellationToken.None);

        await withIcon.RemoveAsync(CancellationToken.None);

        File.Exists(Path.Combine(root, "Torni", "logo.ico")).ShouldBeFalse();
        File.Exists(source).ShouldBeTrue();
    }

    private string ToolIcon(byte[] bytes)
    {
        string source = Path.Combine(root, "tool", "logo.ico");
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllBytes(source, bytes);

        return source;
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}