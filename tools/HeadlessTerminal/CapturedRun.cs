using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// One program running in a pseudo-console of the options' size (120 by 40 unless set): everything it writes is captured, terminal queries are answered
/// when the options say so, and finishing writes the capture to drive-raw.txt and reports whether it exited by itself.
/// Both run modes use it: fixed-delay keystrokes (<see cref="Driver"/>) and marker-led steps (<see cref="StepRunner"/>).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class CapturedRun : IDisposable
{
    private readonly PseudoConsole console;
    private readonly DriverOptions options;
    private readonly TerminalQueries queries;
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly StringBuilder output = new();
    private readonly List<string> replies = [];
    private readonly Task reader;

    private CapturedRun(PseudoConsole console, DriverOptions options)
    {
        this.console = console;
        this.options = options;
        queries = new TerminalQueries(options.XtversionDelayMs);
        reader = Task.Run(ReadAsync);
    }

    public long ElapsedMilliseconds => clock.ElapsedMilliseconds;

    public bool HasExited => console.HasExited;

    public static CapturedRun Start(DriverOptions options, string exe, IReadOnlyList<string> arguments)
        => new(PseudoConsole.Start(exe, arguments, Environment.CurrentDirectory, options.Columns, options.Rows, options.ChildEnvironment()), options);

    public string Captured()
    {
        lock (output)
        {
            return output.ToString();
        }
    }

    public async Task SendAsync(string keys) => await console.WriteAsync(keys);

    /// <summary>
    /// Waits until the program exits or the clock reaches <paramref name="deadlineMs"/>, kills it if it is still
    /// running, writes the capture to <paramref name="captureFile"/> (drive-raw.txt in the current folder unless named) and prints a summary; true when it exited by itself.
    /// </summary>
    public async Task<bool> FinishAsync(long deadlineMs, string captureFile = "drive-raw.txt")
    {
        while (!console.HasExited && clock.ElapsedMilliseconds < deadlineMs)
        {
            await Task.Delay(200);
        }

        bool alive = !console.HasExited;
        string exited = alive ? "null" : $"{{\"exitCode\":{console.ExitCode}}}";
        if (alive)
        {
            console.Kill();
        }

        console.Close();
        await Task.WhenAny(reader, Task.Delay(2000));

        string captured = Captured();
        await File.WriteAllTextAsync(captureFile, captured, new UTF8Encoding(false));
        string plain = EscapeSequences.Strip(captured);
        await Console.Out.WriteLineAsync($"exited: {exited} killed: {(alive ? "true" : "false")} chars: {captured.Length}");
        await Console.Out.WriteLineAsync($"replies sent: {string.Join(' ', replies)}");
        await Console.Out.WriteLineAsync($"last 300 plain chars: {JsonSerializer.Serialize(plain[Math.Max(0, plain.Length - 300)..])}");

        return !alive;
    }

    public void Dispose() => console.Dispose();

    private async Task ReadAsync()
    {
        byte[] buffer = new byte[16384];
        char[] chars = new char[32768];
        Decoder decoder = Encoding.UTF8.GetDecoder();
        while (true)
        {
            int read;
            try
            {
                read = await console.Output.ReadAsync(buffer);
            }
            catch (IOException)
            {
                return;
            }

            if (read == 0)
            {
                return;
            }

            string chunk = new(chars, 0, decoder.GetChars(buffer, 0, read, chars, 0));
            lock (output)
            {
                output.Append(chunk);
            }

            if (options.AnswerQueries)
            {
                Answer(chunk);
            }
        }
    }

    private void Answer(string chunk)
    {
        foreach (TerminalReply reply in queries.RepliesTo(chunk))
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(reply.DelayMs);
                await console.WriteAsync(reply.Text);
                lock (replies)
                {
                    replies.Add($"{JsonSerializer.Serialize(reply.Text)}@{clock.ElapsedMilliseconds}");
                }
            });
        }
    }
}