namespace Source191.Vantage.Application.Views;

/// <summary>What the console screen hands back when it stops: the state to resume with and the effect it left with, <see cref="Quit"/> to exit.</summary>
public sealed record ConsoleExit(ConsoleState State, ViewEffect Effect);