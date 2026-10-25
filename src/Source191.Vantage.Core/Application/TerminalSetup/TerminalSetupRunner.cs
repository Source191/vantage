using Source191.Vantage.Ports;

namespace Source191.Vantage.Application.TerminalSetup;

/// <summary>
/// <c>vantage setup-terminal</c> from start to finish: parse, check the sounds, show what will be written, ask, write
/// the Windows Terminal fragment, then offer Claude Code's terminal bell. Exit 0 when done, declined or nothing to
/// remove, 1 when a write failed, 2 for a usage error. It touches the fragment and one Claude setting, nothing else.
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

    public async Task<int> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        TerminalSetupParse parsed = TerminalSetupCommand.Parse(arguments);
        if (parsed.Request is not TerminalSetupRequest request)
        {
            await ReportProblemAsync(parsed.Problem);
            return 2;
        }

        return request.Remove ? await RemoveAsync(cancellationToken) : await SetUpAsync(request, cancellationToken);
    }

    private async Task<int> RemoveAsync(CancellationToken cancellationToken)
    {
        bool removed = await fragment.RemoveAsync(cancellationToken);
        await output.WriteLineAsync(removed ? Wording.ProfilesRemoved(fragment.Location) : Wording.NothingToRemove);

        return 0;
    }

    private async Task<int> SetUpAsync(TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        TerminalSetupProblem? problem = CheckSounds(request);
        if (problem is not null)
        {
            await ReportProblemAsync(problem);
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

    private TerminalSetupProblem? CheckSounds(TerminalSetupRequest request) => CheckSound(request.Sound) ?? CheckSound(request.SessionSound);

    private TerminalSetupProblem? CheckSound(string? sound)
    {
        return sound switch
        {
            null => null,
            _ when !folders.FileExists(sound) => new TerminalSetupProblem.SoundNotFound(sound),
            _ when !string.Equals(Path.GetExtension(sound), ".wav", StringComparison.OrdinalIgnoreCase) => new TerminalSetupProblem.SoundNotWav(sound),
            _ => null,
        };
    }

    private async Task<TerminalProfiles> ChooseProfilesAsync(TerminalSetupRequest request)
    {
        string? vantageSound = await SoundOrDefaultAsync(request.Sound, VantageDefaultSound, TerminalProfiles.VantageProfileName);
        string? sessionSound = await SoundOrDefaultAsync(request.SessionSound, SessionDefaultSound, TerminalProfiles.SessionProfileName);

        return new TerminalProfiles(vantageExecutable, vantageSound, sessionSound);
    }

    private async Task<string?> SoundOrDefaultAsync(string? given, string defaultFile, string profileName)
    {
        if (given is not null)
        {
            return given;
        }

        string defaultSound = Path.Combine(windowsMediaFolder, defaultFile);
        if (folders.FileExists(defaultSound))
        {
            return defaultSound;
        }

        await output.WriteLineAsync(Wording.DefaultSoundMissing(profileName, defaultSound));

        return null;
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
                return await AskAndEnableBellAsync(request, cancellationToken);
        }
    }

    private async Task<Step> AskAndEnableBellAsync(TerminalSetupRequest request, CancellationToken cancellationToken)
    {
        await output.WriteLineAsync(Wording.ClaudeBellPlan(claudeSettings.Location));
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

    private Task ReportProblemAsync(TerminalSetupProblem? problem) => error.WriteLineAsync(problem is null ? Wording.SetupTerminalUsage : Wording.DescribeSetupProblem(problem));
}