using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Ports;
using System.Text.Json.Nodes;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The real file system behind ITerminalFragment, on a throwaway folder standing in for Windows Terminal's Fragments root.</summary>
[Trait("Category", "Integration")]
public sealed class TerminalFragmentShould : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "vantage-fragments-" + Guid.NewGuid().ToString("N"));
    private readonly TerminalFragment fragment;

    public TerminalFragmentShould()
    {
        fragment = new TerminalFragment(root);
    }

    private static TerminalProfiles Profiles => new(@"C:\Users\me\.dotnet\tools\vantage.exe", new BellSound.File(@"C:\Windows\Media\Windows Notify Messaging.wav"), new BellSound.File(@"C:\Windows\Media\Windows Proximity Notification.wav"));

    private JsonArray WrittenProfiles => JsonNode.Parse(File.ReadAllText(fragment.Location))!["profiles"]!.AsArray();

    [Fact]
    public async Task WriteBothProfilesAsUtf8WithoutABom()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);

        byte[] bytes = await File.ReadAllBytesAsync(fragment.Location, TestContext.Current.CancellationToken);
        bytes.Take(3).ShouldNotBe([(byte)0xEF, (byte)0xBB, (byte)0xBF]);
        fragment.Location.ShouldBe(Path.Combine(root, "Vantage", "vantage.json"));
        JsonArray profiles = WrittenProfiles;
        profiles.Count.ShouldBe(2);
        profiles[0]!["name"]!.GetValue<string>().ShouldBe("Vantage");
        profiles[0]!["commandline"]!.GetValue<string>().ShouldBe("\"C:\\Users\\me\\.dotnet\\tools\\vantage.exe\"");
        profiles[0]!["bellSound"]!.GetValue<string>().ShouldBe(@"C:\Windows\Media\Windows Notify Messaging.wav");
        profiles[1]!["name"]!.GetValue<string>().ShouldBe("Vantage session");
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
        await fragment.WriteAsync(Profiles with { VantageSound = new BellSound.TerminalDefault() }, CancellationToken.None);

        JsonArray profiles = WrittenProfiles;
        profiles[0]!.AsObject().ContainsKey("bellSound").ShouldBeFalse();
        profiles[1]!.AsObject().ContainsKey("bellSound").ShouldBeTrue();
    }

    [Fact]
    public async Task GiveTheVantageProfileNoTabColourAndNoFixedTitle()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);

        JsonObject vantage = WrittenProfiles[0]!.AsObject();
        vantage.ContainsKey("tabColor").ShouldBeFalse();
        vantage.ContainsKey("suppressApplicationTitle").ShouldBeFalse();
    }

    [Fact]
    public async Task ReplaceAnExistingFragment()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);

        await fragment.WriteAsync(Profiles with { VantageSound = new BellSound.File(@"C:\Windows\Media\chimes.wav"), SessionSound = new BellSound.TerminalDefault() }, CancellationToken.None);

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
    public async Task RemoveOnlyTheVantageFolder()
    {
        await fragment.WriteAsync(Profiles, CancellationToken.None);
        string other = Path.Combine(root, "Other", "other.json");
        Directory.CreateDirectory(Path.GetDirectoryName(other)!);
        await File.WriteAllTextAsync(other, "{}", TestContext.Current.CancellationToken);

        bool removed = await fragment.RemoveAsync(CancellationToken.None);

        removed.ShouldBeTrue();
        Directory.Exists(Path.Combine(root, "Vantage")).ShouldBeFalse();
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

        string copy = Path.Combine(root, "Vantage", "vantage.ico");
        (await File.ReadAllBytesAsync(copy, TestContext.Current.CancellationToken)).ShouldBe([1, 2, 3]);
        foreach (JsonNode? profile in WrittenProfiles)
        {
            profile!["icon"]!.GetValue<string>().ShouldBe(copy);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("missing.ico")]
    public async Task WriteNoIconWhenTheToolHasNone(string? source)
    {
        TerminalFragment withoutIcon = new(root, source is null ? null : Path.Combine(root, source));

        await withoutIcon.WriteAsync(Profiles, CancellationToken.None);

        foreach (JsonNode? profile in WrittenProfiles)
        {
            profile!.AsObject().ContainsKey("icon").ShouldBeFalse();
        }
    }

    [Fact]
    public async Task RemoveTheIconWithTheFragment()
    {
        string source = ToolIcon([1]);
        TerminalFragment withIcon = new(root, source);
        await withIcon.WriteAsync(Profiles, CancellationToken.None);

        await withIcon.RemoveAsync(CancellationToken.None);

        File.Exists(Path.Combine(root, "Vantage", "vantage.ico")).ShouldBeFalse();
        File.Exists(source).ShouldBeTrue();
    }

    private string ToolIcon(byte[] bytes)
    {
        string source = Path.Combine(root, "tool", "vantage.ico");
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