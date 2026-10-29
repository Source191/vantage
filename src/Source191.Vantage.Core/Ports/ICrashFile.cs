namespace Source191.Vantage.Ports;

/// <summary>The file a crash leaves behind, <c>%LOCALAPPDATA%\Vantage\last-crash.log</c>, replaced whole by each crash.</summary>
public interface ICrashFile
{
    void Write(string text);
}
