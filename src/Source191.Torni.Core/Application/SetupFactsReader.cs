using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Application.Views;
using Source191.Torni.Ports;
using System.Diagnostics;

namespace Source191.Torni.Application;

/// <summary>
/// Reads the setup the Config tab shows, through the ports that already read it for <c>torni setup-terminal</c> and the
/// update check; a piece whose read fails is unreadable on its own, and never stops the others.
/// </summary>
public sealed class SetupFactsReader(ITerminalFragment fragment, IClaudeSettings claude, IUpdateCheckRecords updateChecks, string settingsFile, DiagnosticLogSetting logSetting, DiagnosticLog log)
{
    private static readonly CodeName This = CodeName.For(typeof(SetupFactsReader));

    public async Task<SetupFacts> ReadAsync(CancellationToken cancellationToken)
    {
        return new SetupFacts(
            new SetupState.At(settingsFile),
            await Guarded(() => Task.FromResult(TerminalProfiles())),
            await Guarded(() => ClaudeBellAsync(cancellationToken)),
            LogState(),
            await Guarded(LastUpdateCheckAsync));
    }

    private Task<SetupState> Guarded(Func<Task<SetupState>> read) => Attempt.RunAsync(log, FailureSite.Load, This, read, exception => exception is IOException or UnauthorizedAccessException, _ => new SetupState.Unreadable());

    private SetupState LogState() => logSetting switch
    {
        DiagnosticLogSetting.File file => new SetupState.At(file.Path),
        DiagnosticLogSetting.Off => new SetupState.Disabled(),
        _ => throw new UnreachableException(),
    };

    private SetupState TerminalProfiles() => fragment.Exists ? new SetupState.At(fragment.Location) : new SetupState.NotSetUp();

    private async Task<SetupState> ClaudeBellAsync(CancellationToken cancellationToken)
    {
        return await claude.ReadNotificationChannelAsync(cancellationToken) switch
        {
            NotificationChannelState.Configured { Value: NotificationChannelState.TerminalBell } => new SetupState.Enabled(),
            NotificationChannelState.Configured other => new SetupState.OtherChannel(other.Value),
            NotificationChannelState.Unreadable => new SetupState.Unreadable(),
            NotificationChannelState.Missing or NotificationChannelState.Unset => new SetupState.NotSetUp(),
            _ => throw new UnreachableException(),
        };
    }

    private async Task<SetupState> LastUpdateCheckAsync()
    {
        return await updateChecks.ReadUpdateCheckAsync() switch
        {
            UpdateCheckRecord.Checked last => new SetupState.Checked(last.CheckedAt, last.Newest),
            UpdateCheckRecord.Never => new SetupState.Never(),
            _ => throw new UnreachableException(),
        };
    }
}