namespace Source191.Torni.ReadmeScene;

/// <summary>
/// Presses keys in the scene's own console by writing key events into its input buffer, where the console's
/// Terminal.Gui driver reads them as if they had been typed; no window focus is needed.
/// </summary>
internal static class ConsoleKeys
{
    private const ushort VirtualKeyEnter = 0x0D;
    private const ushort VirtualKeyEscape = 0x1B;
    private const ushort VirtualKeyTab = 0x09;
    private const ushort VirtualKeyBackspace = 0x08;
    private const ushort VirtualKeyLeft = 0x25;
    private const ushort VirtualKeyUp = 0x26;
    private const ushort VirtualKeyRight = 0x27;
    private const ushort VirtualKeyDown = 0x28;
    private const ushort VirtualKeyF5 = 0x74;
    private const short ShiftInKeyScan = 0x0100;

    public static void Type(char character)
    {
        short scan = NativeMethods.VkKeyScan((ushort)character);
        ushort virtualKey = scan == -1 ? (ushort)0 : (ushort)(scan & 0xFF);
        uint state = scan != -1 && (scan & ShiftInKeyScan) != 0 ? NativeMethods.ShiftPressed : 0;
        Send(virtualKey, character, state);
    }

    public static void Press(NamedKey key)
    {
        (ushort virtualKey, char character) = key switch
        {
            NamedKey.Enter => (VirtualKeyEnter, '\r'),
            NamedKey.Esc => (VirtualKeyEscape, '\u001b'),
            NamedKey.Tab => (VirtualKeyTab, '\t'),
            NamedKey.Backspace => (VirtualKeyBackspace, '\b'),
            NamedKey.Up => (VirtualKeyUp, '\0'),
            NamedKey.Down => (VirtualKeyDown, '\0'),
            NamedKey.Left => (VirtualKeyLeft, '\0'),
            NamedKey.Right => (VirtualKeyRight, '\0'),
            NamedKey.F5 => (VirtualKeyF5, '\0'),
        };
        Send(virtualKey, character, 0);
    }

    private static void Send(ushort virtualKey, char character, uint state)
    {
        ushort scanCode = (ushort)NativeMethods.MapVirtualKey(virtualKey, 0);
        NativeMethods.InputRecord down = new()
        {
            EventType = NativeMethods.KeyEvent,
            KeyDown = 1,
            RepeatCount = 1,
            VirtualKeyCode = virtualKey,
            VirtualScanCode = scanCode,
            UnicodeChar = (ushort)character,
            ControlKeyState = state,
        };
        NativeMethods.InputRecord up = down with { KeyDown = 0 };
        if (!NativeMethods.WriteConsoleInput(NativeMethods.GetStdHandle(NativeMethods.StandardInput), [down, up], 2, out _))
        {
            throw new InvalidOperationException("The scene could not write to its own console's input.");
        }
    }
}