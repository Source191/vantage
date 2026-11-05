namespace Source191.Torni.Application.Views;

/// <summary>What the console screen hands back when it stops: the state it ended in and the effect it left with, from the console always <see cref="Quit"/>.</summary>
public sealed record ConsoleExit(ConsoleState State, ViewEffect Effect);