using System.Runtime.InteropServices;

namespace Source191.Vantage.Adapters.Terminal;

/// <summary>
/// Puts the terminal back into a known state after a child process (claude attach) has owned it.
/// Claude Code leaves focus reporting on and the cursor hidden when it detaches, and its last terminal queries can
/// be answered after it has exited, so the replies would otherwise be read by the next Terminal.Gui start-up.
/// Everything here is idempotent and harmless when nothing needs undoing.
/// </summary>
public static partial class ConsoleRecovery
{
    private const int StandardInputHandle = -10;
    private const int StandardOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    /// <summary>Terminal modes a child may have left behind; each one is turned off, then the cursor is shown.</summary>
    internal const string ResetSequence =
        "\u001b[<u" +          // pop any kitty keyboard protocol flags
        "\u001b[?1004l" +      // focus reporting off
        "\u001b[?2004l" +      // bracketed paste off
        "\u001b[?1000l\u001b[?1002l\u001b[?1003l\u001b[?1006l" + // mouse reporting off
        "\u001b[?2031l" +      // colour scheme notifications off
        "\u001b[?25h";         // cursor visible

    /// <summary>Moves the cursor up one line and erases it: removes a one-line message once a child has given the screen back.</summary>
    public const string EraseLineAbove = "\u001b[1A\u001b[2K";

    /// <summary>How long to wait for late query replies before the input buffer is discarded.</summary>
    internal static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(150);

    /// <summary>Writes escape sequences (such as the tab title) to the console, making sure they are interpreted rather than printed.</summary>
    public static void WriteSequence(TextWriter console, string sequence)
    {
        EnableEscapeSequencesOnOutput();
        console.Write(sequence);
        console.Flush();
    }

    public static void AfterChildProcess(TextWriter console)
    {
        EnableEscapeSequencesOnOutput();
        console.Write(ResetSequence);
        console.Flush();
        Thread.Sleep(SettleTime);
        DiscardPendingInput();
    }

    /// <summary>
    /// Without ENABLE_VIRTUAL_TERMINAL_PROCESSING the reset sequence would print as text. A shell parent turns the
    /// flag on, but Windows Terminal launching vantage.exe directly may not, so it is set here and left on.
    /// </summary>
    private static void EnableEscapeSequencesOnOutput()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        nint output = GetStdHandle(StandardOutputHandle);
        if (output != nint.Zero && output != -1 && GetConsoleMode(output, out uint mode))
        {
            SetConsoleMode(output, mode | EnableVirtualTerminalProcessing);
        }
    }

    private static void DiscardPendingInput()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        nint input = GetStdHandle(StandardInputHandle);
        if (input != nint.Zero && input != -1)
        {
            FlushConsoleInputBuffer(input);
        }
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetStdHandle(int standardHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FlushConsoleInputBuffer(nint consoleInput);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(nint consoleHandle, out uint mode);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(nint consoleHandle, uint mode);
}