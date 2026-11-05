using Source191.Torni.Ports;

namespace Source191.Torni.TestDoubles;

/// <summary>Keeps the crash file's text in memory instead of <c>%LOCALAPPDATA%\Torni\last-crash.log</c>.</summary>
public sealed class FakeCrashFile : ICrashFile
{
    private Exception? failure;

    public string? Written { get; private set; }

    public bool Removed { get; private set; }

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

    public Task<LastCrash> ReadAsync()
    {
        return Task.FromResult<LastCrash>(Written is string text ? new LastCrash.Recorded(text) : new LastCrash.None());
    }

    public Task RemoveAsync()
    {
        Written = null;
        Removed = true;
        return Task.CompletedTask;
    }
}