using Source191.Vantage.Application;
using Source191.Vantage.Application.TerminalSetup;
using Source191.Vantage.Ports;
using Source191.Vantage.TestDoubles;

namespace Source191.Vantage.Core.Tests.Application.TerminalSetup;

public sealed class TerminalSetupRunnerShould : IDisposable
{
    private const string Vantage = @"C:\Users\me\.dotnet\tools\vantage.exe";
    private const string Media = @"C:\Windows\Media";
    private const string NotifyMessaging = @"C:\Windows\Media\Windows Notify Messaging.wav";
    private const string ProximityNotification = @"C:\Windows\Media\Windows Proximity Notification.wav";
    private const string Chimes = @"C:\Windows\Media\chimes.wav";

    private readonly FakeFolders folders = new FakeFolders().WithFile(NotifyMessaging).WithFile(ProximityNotification).WithFile(Chimes).WithFile(@"C:\Windows\Media\notes.txt");
    private readonly FakeTerminalFragment fragment = new();
    private readonly FakeClaudeSettings settings = new();
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();

    private static readonly TerminalSetupRequest Defaults = new(new SoundArgument.Omitted(), new SoundArgument.Omitted(), Yes: false, Remove: false);

    private Task<int> RunAsync(string answers, TerminalSetupRequest? request = null)
    {
        TerminalSetupRunner runner = new(folders, fragment, settings, Vantage, Media, new StringReader(answers), output, error);

        return runner.RunAsync(request ?? Defaults, CancellationToken.None);
    }

    [Fact]
    public async Task ShowTheFragmentAndProfilesAndWriteNothingWhenDeclined()
    {
        int exitCode = await RunAsync("n\nn\n");

        exitCode.ShouldBe(0);
        fragment.Written.ShouldBeEmpty();
        settings.Enabled.ShouldBe(0);
        output.ToString().ShouldContain(Wording.TerminalSetupPlan(fragment.Location, new TerminalProfiles(Vantage, NotifyMessaging, ProximityNotification)));
        output.ToString().ShouldContain(Wording.WriteProfilesQuestion);
        output.ToString().ShouldContain(Wording.NothingWritten);
    }

    [Fact]
    public async Task WriteNothingAtTheEndOfInput()
    {
        int exitCode = await RunAsync(string.Empty);

        exitCode.ShouldBe(0);
        fragment.Written.ShouldBeEmpty();
        settings.Enabled.ShouldBe(0);
        output.ToString().ShouldContain(Wording.NothingWritten);
    }

    [Fact]
    public async Task WriteTheProfilesWithDefaultSoundsAndSayToRestart()
    {
        int exitCode = await RunAsync("y\nn\n");

        exitCode.ShouldBe(0);
        fragment.Written.ShouldBe([new TerminalProfiles(Vantage, NotifyMessaging, ProximityNotification)]);
        output.ToString().ShouldContain(Wording.ProfilesWritten(fragment.Location));
        output.ToString().ShouldNotContain(Wording.NothingWritten);
    }

    [Fact]
    public async Task AcceptAYesAnswerInAnyCase()
    {
        await RunAsync(" YES \nn\n");

        fragment.Written.Count.ShouldBe(1);
    }

    [Fact]
    public async Task WriteGivenSounds()
    {
        int exitCode = await RunAsync(string.Empty, Defaults with { Yes = true, Sound = new SoundArgument.Given(Chimes), SessionSound = new SoundArgument.Given(NotifyMessaging.ToUpperInvariant()) });

        exitCode.ShouldBe(0);
        fragment.Written.Single().ShouldBe(new TerminalProfiles(Vantage, Chimes, NotifyMessaging.ToUpperInvariant()));
    }

    [Fact]
    public async Task WriteARelativeSoundAsAFullPathBecauseWindowsTerminalWillNotShareOurFolder()
    {
        string full = Path.GetFullPath("ding.wav");
        folders.WithFile(full);

        int exitCode = await RunAsync(string.Empty, Defaults with { Yes = true, Sound = new SoundArgument.Given("ding.wav") });

        exitCode.ShouldBe(0);
        fragment.Written.Single().VantageSound.ShouldBe(full);
    }

    [Fact]
    public async Task NameTheValueItReplacesWhenClaudeUsesAnotherChannel()
    {
        FakeClaudeSettings other = new(new NotificationChannelState.Configured("iterm2"));
        TerminalSetupRunner runner = new(folders, fragment, other, Vantage, Media, new StringReader("n\nn\n"), output, error);

        await runner.RunAsync(Defaults, CancellationToken.None);

        output.ToString().ShouldContain(Wording.ClaudeBellPlan(other.Location, replacing: "iterm2"));
    }

    [Fact]
    public async Task ReturnOneWhenTheFragmentCannotBeRemoved()
    {
        fragment.Installed();
        fragment.FailTheRemovalWith = new IOException("in use");

        int exitCode = await RunAsync(string.Empty, Defaults with { Remove = true });

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(Wording.TerminalSetupFailed(fragment.Location, "in use"));
    }

    [Fact]
    public async Task LeaveOutAMissingDefaultAndSaySo()
    {
        FakeFolders withoutDefault = new FakeFolders().WithFile(ProximityNotification);
        TerminalSetupRunner runner = new(withoutDefault, fragment, settings, Vantage, Media, new StringReader("y\nn\n"), output, error);

        await runner.RunAsync(Defaults, CancellationToken.None);

        fragment.Written.Single().ShouldBe(new TerminalProfiles(Vantage, VantageSound: null, ProximityNotification));
        output.ToString().ShouldContain(Wording.DefaultSoundMissing("Vantage", NotifyMessaging));
        output.ToString().ShouldNotContain("Vantage session profile uses");
    }

    [Fact]
    public async Task RefuseAMissingSoundWithExitTwo()
    {
        int exitCode = await RunAsync("y\ny\n", Defaults with { Sound = new SoundArgument.Given(@"C:\Users\me\missing.wav") });

        exitCode.ShouldBe(2);
        error.ToString().ShouldContain(Wording.DescribeSetupProblem(new TerminalSetupProblem.SoundNotFound(@"C:\Users\me\missing.wav")));
        output.ToString().ShouldBeEmpty();
        fragment.Written.ShouldBeEmpty();
        settings.Enabled.ShouldBe(0);
    }

    [Fact]
    public async Task RefuseASoundThatIsNotAWavFileWithExitTwo()
    {
        int exitCode = await RunAsync("y\ny\n", Defaults with { SessionSound = new SoundArgument.Given(@"C:\Windows\Media\notes.txt") });

        exitCode.ShouldBe(2);
        error.ToString().ShouldContain(Wording.DescribeSetupProblem(new TerminalSetupProblem.SoundNotWav(@"C:\Windows\Media\notes.txt")));
        fragment.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefuseABlankSoundAsMissingWithExitTwo()
    {
        int exitCode = await RunAsync("y\ny\n", Defaults with { Sound = new SoundArgument.Given("  ") });

        exitCode.ShouldBe(2);
        error.ToString().ShouldBe(Wording.DescribeSetupProblem(new TerminalSetupProblem.SoundNotFound("  ")) + Environment.NewLine, "blank text is kept as it is, never made a full path");
        fragment.Written.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReportTheVantageSoundsProblemBeforeTheSessionSounds()
    {
        int exitCode = await RunAsync("y\ny\n", Defaults with { Sound = new SoundArgument.Given(@"C:\Windows\Media\notes.txt"), SessionSound = new SoundArgument.Given(@"C:\Users\me\missing.wav") });

        exitCode.ShouldBe(2);
        error.ToString().ShouldBe(Wording.DescribeSetupProblem(new TerminalSetupProblem.SoundNotWav(@"C:\Windows\Media\notes.txt")) + Environment.NewLine, "the first refusal wins");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task RefuseRemoveWithEitherSoundWithExitTwo(bool sound, bool sessionSound)
    {
        SoundArgument chimes = new SoundArgument.Given(Chimes);

        int exitCode = await RunAsync(string.Empty, Defaults with { Remove = true, Sound = sound ? chimes : new SoundArgument.Omitted(), SessionSound = sessionSound ? chimes : new SoundArgument.Omitted() });

        exitCode.ShouldBe(2);
        error.ToString().ShouldBe(Wording.DescribeSetupProblem(new TerminalSetupProblem.RemoveWithSound()) + Environment.NewLine);
        fragment.Removals.ShouldBe(0);
    }

    [Fact]
    public async Task RefuseRemoveWithASoundWithExitTwo()
    {
        int exitCode = await RunAsync(string.Empty, Defaults with { Remove = true, Sound = new SoundArgument.Given(Chimes) });

        exitCode.ShouldBe(2);
        error.ToString().ShouldBe(Wording.DescribeSetupProblem(new TerminalSetupProblem.RemoveWithSound()) + Environment.NewLine);
        fragment.Removals.ShouldBe(0);
        fragment.Written.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("claude_default")]
    public async Task OfferTheTerminalBellWhenItIsNotAlreadySet(string? current)
    {
        FakeClaudeSettings other = new(current is null ? new NotificationChannelState.Unset() : new NotificationChannelState.Configured(current));
        TerminalSetupRunner runner = new(folders, fragment, other, Vantage, Media, new StringReader("n\nn\n"), output, error);

        await runner.RunAsync(Defaults, CancellationToken.None);

        output.ToString().ShouldContain(Wording.ClaudeBellPlan(other.Location, current));
        output.ToString().ShouldContain(Wording.TurnOnClaudeBellQuestion);
    }

    [Fact]
    public async Task OfferTheTerminalBellWhenThereIsNoSettingsFile()
    {
        await RunAsync("n\nn\n");

        output.ToString().ShouldContain(Wording.TurnOnClaudeBellQuestion);
    }

    [Fact]
    public async Task NotOfferTheTerminalBellWhenItIsAlreadyOn()
    {
        FakeClaudeSettings alreadyOn = new(new NotificationChannelState.Configured("terminal_bell"));
        TerminalSetupRunner runner = new(folders, fragment, alreadyOn, Vantage, Media, new StringReader("y\n"), output, error);

        await runner.RunAsync(Defaults, CancellationToken.None);

        output.ToString().ShouldContain(Wording.ClaudeBellAlreadyOn);
        output.ToString().ShouldNotContain(Wording.TurnOnClaudeBellQuestion);
        alreadyOn.Enabled.ShouldBe(0);
    }

    [Fact]
    public async Task EnableTheTerminalBellWhenAccepted()
    {
        int exitCode = await RunAsync("n\ny\n");

        exitCode.ShouldBe(0);
        settings.Enabled.ShouldBe(1);
        fragment.Written.ShouldBeEmpty();
        output.ToString().ShouldContain(Wording.ClaudeBellEnabled(settings.Location));
        output.ToString().ShouldNotContain(Wording.NothingWritten);
    }

    [Fact]
    public async Task SkipAnUnreadableSettingsFileButStillWriteProfiles()
    {
        FakeClaudeSettings unreadable = new(new NotificationChannelState.Unreadable("bad json"));
        TerminalSetupRunner runner = new(folders, fragment, unreadable, Vantage, Media, new StringReader("y\n"), output, error);

        int exitCode = await runner.RunAsync(Defaults, CancellationToken.None);

        exitCode.ShouldBe(0);
        fragment.Written.Count.ShouldBe(1);
        unreadable.Enabled.ShouldBe(0);
        output.ToString().ShouldContain(Wording.ClaudeSettingsUnreadable(unreadable.Location, "bad json"));
    }

    [Fact]
    public async Task AnswerBothQuestionsWithYes()
    {
        int exitCode = await RunAsync(string.Empty, Defaults with { Yes = true });

        exitCode.ShouldBe(0);
        fragment.Written.Count.ShouldBe(1);
        settings.Enabled.ShouldBe(1);
        output.ToString().ShouldNotContain(Wording.WriteProfilesQuestion);
        output.ToString().ShouldNotContain(Wording.TurnOnClaudeBellQuestion);
    }

    [Fact]
    public async Task RemoveTheFragmentAndLeaveClaudeSettingsAlone()
    {
        fragment.Installed();

        int exitCode = await RunAsync(string.Empty, Defaults with { Remove = true });

        exitCode.ShouldBe(0);
        fragment.Exists.ShouldBeFalse();
        settings.Enabled.ShouldBe(0);
        output.ToString().ShouldContain(Wording.ProfilesRemoved(fragment.Location));
    }

    [Fact]
    public async Task ReportNothingToRemove()
    {
        int exitCode = await RunAsync(string.Empty, Defaults with { Remove = true, Yes = true });

        exitCode.ShouldBe(0);
        output.ToString().ShouldContain(Wording.NothingToRemove);
    }

    [Fact]
    public async Task ReturnOneWhenTheFragmentCannotBeWritten()
    {
        fragment.FailTheWriteWith = new IOException("disk full");

        int exitCode = await RunAsync(string.Empty, Defaults with { Yes = true });

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(Wording.TerminalSetupFailed(fragment.Location, "disk full"));
        settings.Enabled.ShouldBe(0);
    }

    [Fact]
    public async Task ReturnOneWhenClaudeSettingsCannotBeWritten()
    {
        settings.FailTheWriteWith = new IOException("locked");

        int exitCode = await RunAsync(string.Empty, Defaults with { Yes = true });

        exitCode.ShouldBe(1);
        error.ToString().ShouldContain(Wording.TerminalSetupFailed(settings.Location, "locked"));
    }

    [Fact]
    public async Task TouchNothingButTheFragmentAndThePreferredNotificationChannel()
    {
        await RunAsync(string.Empty, Defaults with { Yes = true });

        fragment.Written.Count.ShouldBe(1);
        fragment.Removals.ShouldBe(0);
        settings.Enabled.ShouldBe(1);
        settings.State.ShouldBe(new NotificationChannelState.Configured("terminal_bell"));
    }

    public void Dispose()
    {
        output.Dispose();
        error.Dispose();
    }
}