using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;
using System.Diagnostics;

namespace Source191.Vantage.Application;

/// <summary>
/// Reads the setup the Config tab shows, through the ports that already read it for <c>vantage setup-terminal</c> and the
/// update check; a piece whose read fails is unreadable on its own, and never stops the others.
/// </summary>
public sealed class SetupFactsReader(ITerminalFragment fragment, IClaudeSettings claude, IUpdateCheckRecords updateChecks, string settingsFile, DiagnosticLogSetting log)
{
    private const string TerminalBell = "terminal_bell";

    public async Task<SetupFacts> ReadAsync(CancellationToken cancellationToken)
    {
        return new SetupFacts(
            new SetupState.At(settingsFile),
            await Guarded(() => Task.FromResult(TerminalProfiles())),
            await Guarded(() => ClaudeBellAsync(cancellationToken)),
            LogState(),
            await Guarded(LastUpdateCheckAsync));
    }

    private static async Task<SetupState> Guarded(Func<Task<SetupState>> read)
    {
        try
        {
            return await read();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new SetupState.Unreadable();
        }
    }

    private SetupState LogState() => log switch
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
            NotificationChannelState.Configured { Value: TerminalBell } => new SetupState.Enabled(),
            NotificationChannelState.Configured other => new SetupState.OtherChannel(other.Value),
            NotificationChannelState.Unreadable => new SetupState.Unreadable(),
            _ => new SetupState.NotSetUp(),
        };
    }

    private async Task<SetupState> LastUpdateCheckAsync()
    {
        UpdateCheckRecord? record = await updateChecks.ReadUpdateCheckAsync();

        return record is null ? new SetupState.Never() : new SetupState.Checked(record.CheckedAt, record.Newest);
    }
}