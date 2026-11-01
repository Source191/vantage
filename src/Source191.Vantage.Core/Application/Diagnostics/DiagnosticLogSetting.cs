namespace Source191.Vantage.Application.Diagnostics;

/// <summary>
/// What <c>VANTAGE_LOG</c> asks of the diagnostic log, read once as Vantage starts: off, so the log is kept in memory
/// only, or also written to the file it names.
/// </summary>
public abstract record DiagnosticLogSetting
{
    /// <summary>The setting the variable's value gives; unset or blank is off.</summary>
    public static DiagnosticLogSetting Read(string? variable) => string.IsNullOrWhiteSpace(variable) ? new Off() : new File(variable);

    public sealed record Off : DiagnosticLogSetting;

    public sealed record File(string Path) : DiagnosticLogSetting;
}