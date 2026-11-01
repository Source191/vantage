using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>
/// The handful of kernel32 calls a
/// <see href="https://learn.microsoft.com/windows/console/creating-a-pseudoconsole-session">ConPTY host</see> needs:
/// pipes, the <see href="https://learn.microsoft.com/windows/console/createpseudoconsole">pseudo-console</see>,
/// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw">process
/// creation</see> and waiting.
/// </summary>
[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
    public const uint ExtendedStartupInfoPresent = 0x00080000;
    public const uint CreateUnicodeEnvironment = 0x00000400;
    public const int ProcThreadAttributePseudoConsole = 0x00020016;
    public const uint WaitObject0 = 0;
    public const int UseStandardHandles = 0x00000100;

    [StructLayout(LayoutKind.Sequential)]
    public struct Coord
    {
        public short X;
        public short Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StartupInfo
    {
        public int Size;
        public IntPtr Reserved;
        public IntPtr Desktop;
        public IntPtr Title;
        public int X;
        public int Y;
        public int XSize;
        public int YSize;
        public int XCountChars;
        public int YCountChars;
        public int FillAttribute;
        public int Flags;
        public short ShowWindow;
        public short Reserved2;
        public IntPtr Reserved2Pointer;
        public IntPtr StandardInput;
        public IntPtr StandardOutput;
        public IntPtr StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public IntPtr AttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInformation
    {
        public IntPtr Process;
        public IntPtr Thread;
        public int ProcessId;
        public int ThreadId;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreatePipe(out IntPtr readPipe, out IntPtr writePipe, IntPtr pipeAttributes, int size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial int CreatePseudoConsole(Coord size, IntPtr input, IntPtr output, uint flags, out IntPtr pseudoConsole);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void ClosePseudoConsole(IntPtr pseudoConsole);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InitializeProcThreadAttributeList(IntPtr attributeList, int attributeCount, int flags, ref IntPtr size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UpdateProcThreadAttribute(IntPtr attributeList, uint flags, IntPtr attribute, IntPtr value, IntPtr size, IntPtr previousValue, IntPtr returnSize);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial void DeleteProcThreadAttributeList(IntPtr attributeList);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "CreateProcessW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateProcess(string? applicationName, string commandLine, IntPtr processAttributes, IntPtr threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inheritHandles, uint creationFlags, IntPtr environment, string? currentDirectory, ref StartupInfoEx startupInfo, out ProcessInformation processInformation);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint WaitForSingleObject(IntPtr handle, uint milliseconds);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TerminateProcess(IntPtr process, uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(IntPtr handle);
}