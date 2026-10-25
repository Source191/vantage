using Source191.Vantage.Adapters.Claude;
using Source191.Vantage.Ports;
using System.Text;

namespace Source191.Vantage.Infrastructure.Tests;

/// <summary>The real settings file behind IClaudeSettings, in a throwaway folder standing in for ~\.claude.</summary>
[Trait("Category", "Integration")]
public sealed class ClaudeSettingsShould : IDisposable
{
    private readonly string home = Path.Combine(Path.GetTempPath(), "vantage-claude-" + Guid.NewGuid().ToString("N"));
    private readonly ClaudeSettings settings;

    public ClaudeSettingsShould()
    {
        settings = new ClaudeSettings(home);
    }

    private string SettingsFile => Path.Combine(home, "settings.json");

    private async Task GivenAsync(string contents)
    {
        Directory.CreateDirectory(home);
        await File.WriteAllBytesAsync(SettingsFile, new UTF8Encoding(false).GetBytes(contents), TestContext.Current.CancellationToken);
    }

    private async Task<string> ReadAsync() => new UTF8Encoding(false).GetString(await File.ReadAllBytesAsync(SettingsFile, TestContext.Current.CancellationToken));

    [Fact]
    public async Task ReadAMissingFile()
    {
        (await settings.ReadNotificationChannelAsync(CancellationToken.None)).ShouldBe(new NotificationChannelState.Missing());
        settings.Location.ShouldBe(SettingsFile);
    }

    [Fact]
    public async Task ReadAFileWithoutTheSettingAsUnset()
    {
        await GivenAsync("{\n  \"model\": \"opus\"\n}\n");

        (await settings.ReadNotificationChannelAsync(CancellationToken.None)).ShouldBe(new NotificationChannelState.Unset());
    }

    [Fact]
    public async Task ReadTheValueOfTheSetting()
    {
        await GivenAsync("{ \"preferredNotifChannel\": \"terminal_bell\" }");

        (await settings.ReadNotificationChannelAsync(CancellationToken.None)).ShouldBe(new NotificationChannelState.Configured("terminal_bell"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("[1, 2]")]
    [InlineData("{ \"preferredNotifChannel\": ")]
    public async Task ReadAFileThatIsNotAJsonObjectAsUnreadable(string contents)
    {
        await GivenAsync(contents);

        (await settings.ReadNotificationChannelAsync(CancellationToken.None)).ShouldBeOfType<NotificationChannelState.Unreadable>().Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ReadDuplicateKeysAsUnreadable()
    {
        await GivenAsync("{ \"model\": \"a\", \"model\": \"b\" }");

        (await settings.ReadNotificationChannelAsync(CancellationToken.None)).ShouldBeOfType<NotificationChannelState.Unreadable>();
        await Should.ThrowAsync<IOException>(() => settings.EnableTerminalBellAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadAFileAnotherProcessHoldsOpenAsUnreadable()
    {
        await GivenAsync("{}");
        using FileStream held = new(SettingsFile, FileMode.Open, FileAccess.Read, FileShare.None);

        (await settings.ReadNotificationChannelAsync(CancellationToken.None)).ShouldBeOfType<NotificationChannelState.Unreadable>();
    }

    [Fact]
    public async Task LeaveNoTemporaryFileBehind()
    {
        await GivenAsync("{ \"model\": \"opus\" }");

        await settings.EnableTerminalBellAsync(CancellationToken.None);

        Directory.GetFiles(home).Select(Path.GetFileName).ShouldBe(["settings.json"]);
    }

    [Fact]
    public async Task EnableTheTerminalBellKeepingOtherKeysInOrderWithLfOnly()
    {
        await GivenAsync("{\n  \"model\": \"opus\",\n  \"permissions\": {\n    \"allow\": [\"Bash(git status)\"]\n  },\n  \"theme\": \"dark\"\n}\n");

        await settings.EnableTerminalBellAsync(CancellationToken.None);

        string written = await ReadAsync();
        written.ShouldBe("{\n  \"model\": \"opus\",\n  \"permissions\": {\n    \"allow\": [\n      \"Bash(git status)\"\n    ]\n  },\n  \"theme\": \"dark\",\n  \"preferredNotifChannel\": \"terminal_bell\"\n}\n");
        (await File.ReadAllBytesAsync(SettingsFile, TestContext.Current.CancellationToken)).ShouldNotContain((byte)'\r');
    }

    [Fact]
    public async Task ChangeAnExistingValueInPlace()
    {
        await GivenAsync("{\n  \"preferredNotifChannel\": \"auto\",\n  \"model\": \"opus\"\n}\n");

        await settings.EnableTerminalBellAsync(CancellationToken.None);

        (await ReadAsync()).ShouldBe("{\n  \"preferredNotifChannel\": \"terminal_bell\",\n  \"model\": \"opus\"\n}\n");
    }

    [Fact]
    public async Task CreateTheFileWhenMissing()
    {
        await settings.EnableTerminalBellAsync(CancellationToken.None);

        (await ReadAsync()).ShouldBe("{\n  \"preferredNotifChannel\": \"terminal_bell\"\n}\n");
    }

    [Fact]
    public async Task KeepTheTrailingNewlineAsItWas()
    {
        await GivenAsync("{\"model\":\"opus\"}");

        await settings.EnableTerminalBellAsync(CancellationToken.None);

        (await ReadAsync()).ShouldBe("{\n  \"model\": \"opus\",\n  \"preferredNotifChannel\": \"terminal_bell\"\n}");
    }

    [Fact]
    public async Task KeepTextAsItWasWrittenWithoutEscapingIt()
    {
        await GivenAsync("{\n  \"statusLine\": \"a < b & 'c' é\"\n}\n");

        await settings.EnableTerminalBellAsync(CancellationToken.None);

        (await ReadAsync()).ShouldContain("\"a < b & 'c' é\"");
    }

    [Fact]
    public async Task NeverTouchAFileThatIsNotValidJson()
    {
        await GivenAsync("{ broken");

        await Should.ThrowAsync<IOException>(() => settings.EnableTerminalBellAsync(CancellationToken.None));

        (await ReadAsync()).ShouldBe("{ broken");
    }

    public void Dispose()
    {
        if (Directory.Exists(home))
        {
            Directory.Delete(home, recursive: true);
        }
    }
}