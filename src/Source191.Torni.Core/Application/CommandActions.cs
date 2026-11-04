using Source191.Torni.Application.Launch;
using Source191.Torni.Application.TerminalSetup;

namespace Source191.Torni.Application;

/// <summary>What each command does once the command line is parsed; each returns the exit code. Program.cs supplies them.</summary>
public sealed record CommandActions(
    Func<CancellationToken, Task<int>> OpenConsole,
    Func<HereName, string, CancellationToken, Task<int>> StartHere,
    Func<TerminalSetupRequest, CancellationToken, Task<int>> SetUpTerminal,
    Func<Guid, int, CancellationToken, Task<int>> ClaimTab);