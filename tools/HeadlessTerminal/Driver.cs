using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>How a drive runs: whether to pretend to be Windows Terminal and answer its queries, and the XTVERSION delay.</summary>
public sealed record DriverOptions(bool PretendWindowsTerminal, bool AnswerQueries, int XtversionDelayMs)
{
    public static DriverOptions FromEnvironment()
    {
        return new DriverOptions(
            PretendWindowsTerminal: Environment.GetEnvironmentVariable("HARNESS_PLAIN") is null,
            AnswerQueries: Environment.GetEnvironmentVariable("HARNESS_RESPOND") is not null,
            XtversionDelayMs: int.TryParse(Environment.GetEnvironmentVariable("HARNESS_DCS_DELAY_MS"), out int delay) ? delay : 0);
    }

    public IReadOnlyDictionary<string, string> ChildEnvironment()
    {
        Dictionary<string, string> environment = new(StringComparer.OrdinalIgnoreCase);
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            environment[(string)entry.Key] = (string?)entry.Value ?? string.Empty;
        }

        environment["VANTAGE_NO_UPDATE_CHECK"] = "1";

        if (PretendWindowsTerminal)
        {
            environment["TERM_PROGRAM"] = "WindowsTerminal";
            environment["WT_SESSION"] = "harness";
        }
        else
        {
            environment.Remove("TERM_PROGRAM");
            environment.Remove("WT_SESSION");
        }

        return environment;
    }
}

/// <summary>
/// Runs a program in a pseudo-console, sends the scripted keystrokes on time, answers terminal queries, captures all
/// output to drive-raw.txt, and reports whether the program exited by itself within six seconds of the last key.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Driver(DriverOptions options)
{
    private const int Columns = 120;
    private const int Rows = 40;
    private const int GraceAfterLastKeyMs = 6000;

    private readonly TerminalQueries queries = new(options.XtversionDelayMs);
    private readonly StringBuilder output = new();
    private readonly List<string> replies = [];

    public async Task<int> RunAsync(string exe, IReadOnlyList<Keystroke> script, IReadOnlyList<string> arguments)
    {
        using PseudoConsole console = PseudoConsole.Start(exe, arguments, Environment.CurrentDirectory, Columns, Rows, options.ChildEnvironment());
        Stopwatch clock = Stopwatch.StartNew();
        Task reader = Task.Run(() => ReadAsync(console, clock));

        foreach (Keystroke step in script)
        {
            await WaitUntil(clock, step.At);
            if (console.HasExited)
            {
                break;
            }

            await console.WriteAsync(step.Keys);
            await Console.Out.WriteLineAsync($"t={step.At}ms sent {JsonSerializer.Serialize(step.Keys)} (output so far {Captured().Length} chars)");
        }

        int deadline = (script.Count == 0 ? 0 : script[^1].At) + GraceAfterLastKeyMs;
        while (!console.HasExited && clock.ElapsedMilliseconds < deadline)
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
        await File.WriteAllTextAsync("drive-raw.txt", captured, new UTF8Encoding(false));
        string plain = EscapeSequences.Strip(captured);
        await Console.Out.WriteLineAsync($"exited: {exited} killed: {(alive ? "true" : "false")} chars: {captured.Length}");
        await Console.Out.WriteLineAsync($"replies sent: {string.Join(' ', replies)}");
        await Console.Out.WriteLineAsync($"last 300 plain chars: {JsonSerializer.Serialize(plain[Math.Max(0, plain.Length - 300)..])}");

        return alive ? 1 : 0;
    }

    private async Task ReadAsync(PseudoConsole console, Stopwatch clock)
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
                Answer(console, chunk, clock);
            }
        }
    }

    private void Answer(PseudoConsole console, string chunk, Stopwatch clock)
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

    private string Captured()
    {
        lock (output)
        {
            return output.ToString();
        }
    }

    private static async Task WaitUntil(Stopwatch clock, int at)
    {
        int wait = at - (int)clock.ElapsedMilliseconds;
        if (wait > 0)
        {
            await Task.Delay(wait);
        }
    }
}