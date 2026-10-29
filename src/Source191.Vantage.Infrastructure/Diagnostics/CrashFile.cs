using Source191.Vantage.Ports;
using System.Text;

namespace Source191.Vantage.Adapters.Diagnostics;

/// <summary><c>last-crash.log</c> in Vantage's state folder, replaced whole by each crash (UTF-8, no byte order mark).</summary>
public sealed class CrashFile(string folder) : ICrashFile
{
    private const string Name = "last-crash.log";

    public void Write(string text)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, Name), text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
