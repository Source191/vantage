using Source191.Vantage.Domain;

namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// One thing Vantage records about its own behaviour. The set is closed and every member is a number, a duration, a
/// version, an enum or a value built only from Vantage's code (<see cref="CodeName"/>, <see cref="ExceptionSummary"/>,
/// <see cref="ChildCommand"/>); <c>DiagnosticEventsShould</c> fails when a case could hold anything else.
/// </summary>
public abstract record DiagnosticEvent;

public sealed record Started(ToolVersion Vantage, Version? ClaudeCode, Version Windows, Version Runtime, int Columns, int Rows) : DiagnosticEvent;

public sealed record ViewShown(ViewKind View) : DiagnosticEvent;

public sealed record DialogShown(DialogName Dialog) : DiagnosticEvent;

public sealed record EffectRan(CodeName Effect, CodeName Outcome, TimeSpan Duration) : DiagnosticEvent;


public sealed record ProcessSlow(ChildCommand Command, int ExitCode, TimeSpan Duration) : DiagnosticEvent;

public sealed record ProcessFailedToStart(ChildCommand Command, ExceptionSummary Failure) : DiagnosticEvent;

/// <summary>A failure caught where it was turned into a status message, a load failure or a crash; <paramref name="What"/> is what was running, when there was one thing.</summary>
public sealed record Failed(FailureSite Site, CodeName? What, ExceptionSummary Failure) : DiagnosticEvent;

/// <summary>A Terminal.Gui warning or error, with none of Terminal.Gui's words: its messages can quote input it failed to parse.</summary>
public sealed record TerminalGuiReported(DiagnosticLevel Level, ExceptionSummary? Failure) : DiagnosticEvent;

public sealed record TimedEvent(DateTimeOffset At, DiagnosticEvent Event);

public enum ViewKind
{
    Sessions,
    Worktrees,
    Config,
}

public enum FailureSite
{
    Load,
    Effect,
    UpdateCheck,
    BackgroundTask,
    Crash,
}

public enum DiagnosticLevel
{
    Warning,
    Error,
    Critical,
}