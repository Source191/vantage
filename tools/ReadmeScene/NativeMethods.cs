using System.Runtime.InteropServices;
using System.Text;

namespace Source191.Torni.ReadmeScene;

/// <summary>The Win32 calls the scene makes: finding its Windows Terminal window by title and capturing it.</summary>
internal static partial class NativeMethods
{
    public const uint PrintWindowRenderFullContent = 2;

    public delegate bool EnumWindowsProc(nint window, nint parameter);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetWindowText(nint window, [Out] char[] text, int maximum);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetClassName(nint window, [Out] char[] text, int maximum);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PrintWindow(nint window, nint deviceContext, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowRect(nint window, out Rectangle rectangle);

    /// <summary>DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2, so a capture is at the window's real pixel size.</summary>
    public static readonly nint PerMonitorAwareV2 = -4;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetProcessDpiAwarenessContext(nint context);

    public const int StandardInput = -10;
    public const ushort KeyEvent = 1;
    public const uint ShiftPressed = 0x0010;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint GetStdHandle(int handle);

    [LibraryImport("kernel32.dll", EntryPoint = "WriteConsoleInputW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteConsoleInput(nint input, [In] InputRecord[] records, uint count, out uint written);

    [LibraryImport("user32.dll", EntryPoint = "VkKeyScanW")]
    public static partial short VkKeyScan(ushort character);

    [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyW")]
    public static partial uint MapVirtualKey(uint code, uint mapType);

    /// <summary>INPUT_RECORD holding a KEY_EVENT_RECORD, the only kind the scene writes.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct InputRecord
    {
        [FieldOffset(0)]
        public ushort EventType;

        [FieldOffset(4)]
        public int KeyDown;

        [FieldOffset(8)]
        public ushort RepeatCount;

        [FieldOffset(10)]
        public ushort VirtualKeyCode;

        [FieldOffset(12)]
        public ushort VirtualScanCode;

        [FieldOffset(14)]
        public ushort UnicodeChar;

        [FieldOffset(16)]
        public uint ControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static string TextOf(nint window, Func<nint, char[], int, int> read)
    {
        char[] buffer = new char[256];
        int length = read(window, buffer, buffer.Length);

        return new StringBuilder().Append(buffer, 0, Math.Max(length, 0)).ToString();
    }
}