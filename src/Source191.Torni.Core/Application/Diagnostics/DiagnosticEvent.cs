namespace Source191.Torni.Application.Diagnostics;

/// <summary>
/// One thing the console records about its own behaviour. The set is closed and every member is a number, a duration, a
/// version, an enum or a value built only from its own code (<see cref="CodeName"/>, <see cref="ExceptionSummary"/>,
/// <see cref="ChildCommand"/>); <c>DiagnosticEventsShould</c> fails when a case could hold anything else.
/// </summary>
public abstract record DiagnosticEvent;