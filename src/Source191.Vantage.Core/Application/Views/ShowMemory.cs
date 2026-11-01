using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Views;

/// <summary>Measure the session's process tree and show what its memory is made of; the process id is the tree's root.</summary>
public sealed record ShowMemory(Session Session, int ProcessId) : ViewEffect, IDialogEffect;