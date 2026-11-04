namespace Source191.Torni.Application.Diagnostics;

/// <summary>
/// One thing Torni records about its own behaviour. The set is closed and every member is a number, a duration, a
/// version, an enum or a value built only from Torni's code (<see cref="CodeName"/>, <see cref="ExceptionSummary"/>,
/// <see cref="ChildCommand"/>); <c>DiagnosticEventsShould</c> fails when a case could hold anything else.
/// </summary>
public abstract record DiagnosticEvent;