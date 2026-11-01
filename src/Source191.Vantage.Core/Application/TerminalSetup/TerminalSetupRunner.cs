using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>
/// <c>vantage setup-terminal</c> from start to finish, once the command line is parsed: check the request and the sounds, show what will be written, ask, write
/// the Windows Terminal fragment, then offer Claude Code's terminal bell. Exit 0 when done, declined or nothing to
/// remove, 1 when a write failed, 2 for a refused request (--remove with a sound, a missing or non-wav sound). It touches the fragment and one Claude setting, nothing else.
/// </summary>
public sealed class TerminalSetupRunner(
    IFolders folders,
    ITerminalFragment fragment,
    IClaudeSettings claudeSettings,
    string vantageExecutable,
    string windowsMediaFolder,
    TextReader input,
    TextWriter output,
    TextWriter error)
{
    private const string VantageDefaultSound = "Windows Notify Messaging.wav";
    private const string SessionDefaultSound = "Windows Proximity Notification.wav";
    private const string TerminalBell = "terminal_bell";

    private enum Step
    {
        Skipped,
        Written,
        Failed,
    }

    public async Task<int> RunAsync(TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        if (request.Remove && (request.Sound is SoundArgument.Given || request.SessionSound is SoundArgument.Given))
        {
            await ReportProblemAsync(new TerminalSetupProblem.RemoveWithSound());
            return 2;
        }

        return request.Remove ? await RemoveAsync(cancellationToken) : await SetUpAsync(request with { Sound = FullPath(request.Sound), SessionSound = FullPath(request.SessionSound) }, cancellationToken);
    }

    /// <summary>Windows Terminal does not share our working directory, so a sound is written as a full path.</summary>
    private static SoundArgument FullPath(SoundArgument sound) => sound switch
    {
        SoundArgument.Given given when !string.IsNullOrWhiteSpace(given.Path) => new SoundArgument.Given(Path.GetFullPath(given.Path)),
        _ => sound,
    };

    private async Task<int> RemoveAsync(CancellationToken cancellationToken)
    {
        bool removed = false;
        Step step = await TryWriteAsync(fragment.Location, async () => removed = await fragment.RemoveAsync(cancellationToken));
        if (step == Step.Failed)
        {
            return 1;
        }

        await output.WriteLineAsync(removed ? Wording.ProfilesRemoved(fragment.Location) : Wording.NothingToRemove);

        return 0;
    }

    private async Task<int> SetUpAsync(TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        if (CheckSounds(request) is SoundCheck.Refused refused)
        {
            await ReportProblemAsync(refused.Problem);
            return 2;
        }

        TerminalProfiles profiles = await ChooseProfilesAsync(request);
        await output.WriteLineAsync(Wording.TerminalSetupPlan(fragment.Location, profiles));
        Step profileStep = await WriteProfilesAsync(request, profiles, cancellationToken);
        Step bellStep = profileStep == Step.Failed ? Step.Skipped : await OfferBellAsync(request, cancellationToken);
        if (profileStep == Step.Failed || bellStep == Step.Failed)
        {
            return 1;
        }

        if (profileStep == Step.Skipped && bellStep == Step.Skipped)
        {
            await output.WriteLineAsync(Wording.NothingWritten);
        }

        return 0;
    }

    /// <summary>The Vantage profile's sound is checked first, and its refusal is the one reported.</summary>
    private SoundCheck CheckSounds(TerminalSetupRequest request) => CheckSound(request.Sound) is SoundCheck.Refused refused ? refused : CheckSound(request.SessionSound);

    private SoundCheck CheckSound(SoundArgument sound)
    {
        return sound switch
        {
            SoundArgument.Omitted => new SoundCheck.Accepted(),
            SoundArgument.Given given when !folders.FileExists(given.Path) => new SoundCheck.Refused(new TerminalSetupProblem.SoundNotFound(given.Path)),
            SoundArgument.Given given when !string.Equals(Path.GetExtension(given.Path), ".wav", StringComparison.OrdinalIgnoreCase) => new SoundCheck.Refused(new TerminalSetupProblem.SoundNotWav(given.Path)),
            SoundArgument.Given => new SoundCheck.Accepted(),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<TerminalProfiles> ChooseProfilesAsync(TerminalSetupRequest request)
    {
        BellSound vantageSound = await SoundOrDefaultAsync(request.Sound, VantageDefaultSound, TerminalProfiles.VantageProfileName);
        BellSound sessionSound = await SoundOrDefaultAsync(request.SessionSound, SessionDefaultSound, TerminalProfiles.SessionProfileName);

        return new TerminalProfiles(vantageExecutable, vantageSound, sessionSound);
    }

    /// <summary>The sound given, else the Windows media default, else Windows Terminal's own after saying so.</summary>
    private async Task<BellSound> SoundOrDefaultAsync(SoundArgument sound, string defaultFile, string profileName)
    {
        if (sound is SoundArgument.Given given)
        {
            return new BellSound.File(given.Path);
        }

        string defaultSound = Path.Combine(windowsMediaFolder, defaultFile);
        if (folders.FileExists(defaultSound))
        {
            return new BellSound.File(defaultSound);
        }

        await output.WriteLineAsync(Wording.DefaultSoundMissing(profileName, defaultSound));

        return new BellSound.TerminalDefault();
    }

    private async Task<Step> WriteProfilesAsync(TerminalSetupRequest request, TerminalProfiles profiles, CancellationToken cancellationToken)
    {
        if (!await ConfirmedAsync(request, Wording.WriteProfilesQuestion))
        {
            return Step.Skipped;
        }

        Step step = await TryWriteAsync(fragment.Location, () => fragment.WriteAsync(profiles, cancellationToken));
        if (step == Step.Written)
        {
            await output.WriteLineAsync(Wording.ProfilesWritten(fragment.Location));
        }

        return step;
    }

    private async Task<Step> OfferBellAsync(TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        NotificationChannelState state = await claudeSettings.ReadNotificationChannelAsync(cancellationToken);
        switch (state)
        {
            case NotificationChannelState.Configured { Value: TerminalBell }:
                await output.WriteLineAsync(Wording.ClaudeBellAlreadyOn);
                return Step.Skipped;
            case NotificationChannelState.Unreadable unreadable:
                await output.WriteLineAsync(Wording.ClaudeSettingsUnreadable(claudeSettings.Location, unreadable.Reason));
                return Step.Skipped;
            default:
                return await AskAndEnableBellAsync(state, request, cancellationToken);
        }
    }

    private async Task<Step> AskAndEnableBellAsync(NotificationChannelState state, TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        string? replacing = (state as NotificationChannelState.Configured)?.Value;
        await output.WriteLineAsync(Wording.ClaudeBellPlan(claudeSettings.Location, replacing));
        if (!await ConfirmedAsync(request, Wording.TurnOnClaudeBellQuestion))
        {
            return Step.Skipped;
        }

        Step step = await TryWriteAsync(claudeSettings.Location, () => claudeSettings.EnableTerminalBellAsync(cancellationToken));
        if (step == Step.Written)
        {
            await output.WriteLineAsync(Wording.ClaudeBellEnabled(claudeSettings.Location));
        }

        return step;
    }

    private async Task<Step> TryWriteAsync(string path, Func<Task> write)
    {
        try
        {
            await write();

            return Step.Written;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The message reports what failed; it is never offered to the user as a reason to change something.
            await error.WriteLineAsync(Wording.TerminalSetupFailed(path, exception.Message));

            return Step.Failed;
        }
    }

    private async Task<bool> ConfirmedAsync(TerminalSetupRequest request, string question)
    {
        if (request.Yes)
        {
            return true;
        }

        await output.WriteAsync(question);
        string? answer = await input.ReadLineAsync();
        await output.WriteLineAsync();

        return answer?.Trim() is { } text && (string.Equals(text, "y", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase));
    }

    private Task ReportProblemAsync(TerminalSetupProblem problem) => error.WriteLineAsync(Wording.DescribeSetupProblem(problem));

    /// <summary>A given sound accepted, or the problem that refuses it; an omitted one is always accepted.</summary>
    private abstract record SoundCheck
    {
        public sealed record Accepted : SoundCheck;

        public sealed record Refused(TerminalSetupProblem Problem) : SoundCheck;
    }
}