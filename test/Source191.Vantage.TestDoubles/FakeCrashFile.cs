using Source191.Vantage.Ports;

namespace Source191.Vantage.TestDoubles;

/// <summary>Keeps the crash file's text in memory instead of <c>%LOCALAPPDATA%\Vantage\last-crash.log</c>.</summary>
public sealed class FakeCrashFile : ICrashFile
{
    private Exception? failure;

    public string? Written { get; private set; }

    public FakeCrashFile FailWith(Exception exception)
    {
        failure = exception;
        return this;
    }

    public void Write(string text)
    {
        if (failure is not null)
        {
            throw failure;
        }

        Written = text;
    }
}
