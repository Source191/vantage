using Source191.Vantage.Ports;
using System.Text;

namespace Source191.Vantage.Adapters.Diagnostics;

/// <summary><c>last-crash.log</c> in Vantage's state folder, replaced whole by each crash (UTF-8, no byte order mark).</summary>
public sealed class CrashFile(string folder) : ICrashFile
{
    private const string Name = "last-crash.log";

    private string FilePath => Path.Combine(folder, Name);

    public void Write(string text)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public async Task<LastCrash> ReadAsync() => File.Exists(FilePath) ? new LastCrash.Recorded(await File.ReadAllTextAsync(FilePath)) : new LastCrash.None();

    public Task RemoveAsync()
    {
        // File.Delete ignores a missing file but not a missing folder.
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }

        return Task.CompletedTask;
    }
}