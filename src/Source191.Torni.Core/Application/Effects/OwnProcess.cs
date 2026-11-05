using Source191.Torni.Domain;
using Source191.Torni.Ports;

namespace Source191.Torni.Application.Effects;

/// <summary>This Torni's process, which the update tab waits for, the folder that tab starts in, and winget, which upgrades it.</summary>
public sealed record OwnProcess(int ProcessId, FolderPath Home, ProgramLocation Winget);