using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source191.Torni.Adapters.Processes;

public sealed partial class ProcessRegistry
{
    private const uint SnapshotOfProcesses = 0x2;
    private const int ProcessBasicInformationClass = 0;
    private const int ProcessCommandLineInformationClass = 60;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
    private const uint ProcessQueryLimitedInformation = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public nint Reserved1;
        public nint PebBaseAddress;
        public nint Reserved2a;
        public nint Reserved2b;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public nint Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessEntry
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public nuint DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;
        public ExecutableName ExeFile;
    }

    /// <summary>
    /// <see href="https://learn.microsoft.com/windows/win32/api/tlhelp32/ns-tlhelp32-processentry32w">PROCESSENTRY32W</see>'s
    /// szExeFile: 260 UTF-16 units ending at the first zero. The units are ushort rather than char so
    /// the structure stays blittable, which the source-generated calls require. Internal rather than private because Sonar
    /// reads an inline array's one declared field as unused (S1144) when the type is private.
    /// </summary>
    [InlineArray(260)]
    internal struct ExecutableName
    {
        public ushort Unit;

        public override readonly string ToString()
        {
            ReadOnlySpan<char> text = MemoryMarshal.Cast<ushort, char>(this);
            int end = text.IndexOf('\0');

            return new string(end < 0 ? text : text[..end]);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCountersEx2
    {
        public uint Size;
        public uint PageFaultCount;
        public nuint PeakWorkingSetSize;
        public nuint WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage;
        public nuint QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage;
        public nuint QuotaNonPagedPoolUsage;
        public nuint PagefileUsage;
        public nuint PeakPagefileUsage;
        public nuint PrivateUsage;
        public nuint PrivateWorkingSetSize;
        public ulong SharedCommitUsage;
    }

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(nint processHandle, int processInformationClass, out ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(nint processHandle, int processInformationClass, nint processInformation, int processInformationLength, out int returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateProcess(nint processHandle, uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint CreateToolhelp32Snapshot(uint flags, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessTimes(nint processHandle, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryFullProcessImageNameW(nint processHandle, uint flags, [Out] char[] imageName, ref int size);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Process32FirstW(nint snapshot, ref ProcessEntry entry);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Process32NextW(nint snapshot, ref ProcessEntry entry);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(nint handle);

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessMemoryInfo(nint processHandle, ref ProcessMemoryCountersEx2 counters, uint size);
}