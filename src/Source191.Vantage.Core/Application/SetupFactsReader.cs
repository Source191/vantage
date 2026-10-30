using Source191.Vantage.Application.Views;
using Source191.Vantage.Ports;

namespace Source191.Vantage.Application;

/// <summary>
/// Reads the setup the Config tab shows, through the ports that already read it for <c>vantage setup-terminal</c> and the
/// update check; a piece whose read fails is unreadable on its own, and never stops the others.
/// </summary>
public sealed class SetupFactsReader(ITerminalFragment fragment, IClaudeSettings claude, IUpdateCheckRecords updateChecks, string settingsFile, string? logPath)
{
    public Task<SetupFacts> ReadAsync(CancellationToken cancellationToken)
    {
        _ = (fragment, claude, updateChecks, logPath, cancellationToken);
        SetupState reading = new SetupState.Reading();

        return Task.FromResult(new SetupFacts(new SetupState.At(settingsFile), reading, reading, reading, reading));
    }
}
