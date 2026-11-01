namespace Source191.Vantage.Ports;

/// <summary>The file a crash leaves behind, <c>%LOCALAPPDATA%\Vantage\last-crash.log</c>, replaced whole by each crash.</summary>
public interface ICrashFile
{
    void Write(string text);

    /// <summary>The last crash's text, or <see cref="LastCrash.None"/> when there is none.</summary>
    Task<LastCrash> ReadAsync();

    /// <summary>Removes the crash once a report has carried it; a file that has already gone is fine.</summary>
    Task RemoveAsync();
}