using Source191.Torni.Application.Diagnostics;

namespace Source191.Torni.Core.Tests.Application.Reporting;

/// <summary>The line a crash leaves in the crash file, rendered by <see cref="DiagnosticLine"/> as Torni writes it.</summary>
internal static class CrashLine
{
    public static string At(DateTimeOffset moment) => DiagnosticLine.Render(new TimedEvent(moment, new Failed(FailureSite.Crash, null, ExceptionSummary.From(new InvalidOperationException()))));
}