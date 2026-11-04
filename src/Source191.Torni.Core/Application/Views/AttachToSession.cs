using Source191.Torni.Domain;

namespace Source191.Torni.Application.Views;

/// <summary>
/// Attach to a session in this terminal: an <see cref="ILeavesScreen"/> only <c>torni here</c> raises;
/// <see cref="ScreenLoop"/> refuses it from the console, which never hands its terminal to a child.
/// </summary>
public sealed record AttachToSession(JobId JobId) : ViewEffect, ILeavesScreen;