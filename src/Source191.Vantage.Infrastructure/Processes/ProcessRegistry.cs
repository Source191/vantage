using Source191.Vantage.Domain;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Reads process start times, parents, the whole table and what a process uses from the operating system (times in
/// UTC so a value survives a JSON round trip unchanged), and ends processes either with their whole tree or cleanly
/// with exit code 0.
/// </summary>
public sealed class ProcessRegistry : IProcessRegistry
{
    private const uint SnapshotOfProcesses = 0x2;
    private const int ProcessBasicInformationClass = 0;
    private const int ProcessCommandLineInformationClass = 60;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public DateTimeOffset? StartTimeOf(int processId) => WithProcess<DateTimeOffset?>(processId, process => new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero));

    public void Terminate(int processId)
    {
        WithProcess<object?>(processId, process =>
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(TimeSpan.FromSeconds(5));
            return null;
        });
    }

    public ProcessParent? ParentOf(int processId)
    {
        return WithProcess<ProcessParent?>(processId, process =>
        {
            int status = NtQueryInformationProcess(process.Handle, ProcessBasicInformationClass, out ProcessBasicInformation information, Marshal.SizeOf<ProcessBasicInformation>(), out _);
            long parentId = information.InheritedFromUniqueProcessId.ToInt64();

            return status != 0 || parentId <= 0 || parentId > int.MaxValue
                ? null
                : WithProcess<ProcessParent?>((int)parentId, parent => new ProcessParent((int)parentId, parent.ProcessName));
        });
    }

    public void EndCleanly(int processId)
    {
        WithProcess<object?>(processId, process =>
        {
            if (TerminateProcess(process.Handle, 0))
            {
                process.WaitForExit(TimeSpan.FromSeconds(5));
            }

            return null;
        });
    }

    /// <summary>One Toolhelp snapshot: every process with its parent's id, no handles opened, a few milliseconds for hundreds of processes.</summary>
    public IReadOnlyList<ProcessFacts> Snapshot()
    {
        IntPtr snapshot = CreateToolhelp32Snapshot(SnapshotOfProcesses, 0);
        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
        {
            return [];
        }

        try
        {
            return ReadEntries(snapshot);
        }
        finally
        {
            CloseHandle(snapshot);
        }
    }

    /// <summary>
    /// The private working set (Task Manager's Memory column, shared pages excluded), the start time and the command
    /// line, from one limited-rights handle opened directly: <see cref="Process.GetProcessById(int)"/> walks the whole
    /// table on every call, about twenty milliseconds, and a refresh measures a few hundred processes. The command
    /// line is read for naming only and is never written anywhere.
    /// </summary>
    public ProcessUsage? UsageOf(int processId)
    {
        IntPtr handle = OpenProcess(ProcessQueryLimitedInformation, false, (uint)processId);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            // A process that has ended but is still held open has an exit time; a running one has none.
            if (!GetProcessTimes(handle, out long creation, out long exit, out _, out _) || exit != 0)
            {
                return null;
            }

            DateTimeOffset startedAt = new(DateTime.FromFileTimeUtc(creation), TimeSpan.Zero);

            return new ProcessUsage(startedAt, PrivateMemoryOf(handle), CommandLineOf(handle));
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    private static List<ProcessFacts> ReadEntries(IntPtr snapshot)
    {
        List<ProcessFacts> entries = [];
        ProcessEntry entry = new() { Size = (uint)Marshal.SizeOf<ProcessEntry>() };
        bool more = Process32FirstW(snapshot, ref entry);
        while (more)
        {
            entries.Add(new ProcessFacts((int)entry.ProcessId, (int)entry.ParentProcessId, Path.GetFileNameWithoutExtension(entry.ExeFile)));
            more = Process32NextW(snapshot, ref entry);
        }

        return entries;
    }

    /// <summary>The private working set when the operating system reports it (Windows 10 2004 onwards), else the working set from the older structure, else nothing.</summary>
    private static MemorySize PrivateMemoryOf(IntPtr handle)
    {
        ProcessMemoryCountersEx2 counters = new() { Size = (uint)Marshal.SizeOf<ProcessMemoryCountersEx2>() };
        if (GetProcessMemoryInfo(handle, ref counters, counters.Size))
        {
            return MemorySize.FromBytes((long)counters.PrivateWorkingSetSize.ToUInt64());
        }

        counters.Size = (uint)Marshal.OffsetOf<ProcessMemoryCountersEx2>(nameof(ProcessMemoryCountersEx2.PrivateWorkingSetSize)).ToInt32();

        return GetProcessMemoryInfo(handle, ref counters, counters.Size)
            ? MemorySize.FromBytes((long)counters.WorkingSetSize.ToUInt64())
            : MemorySize.Zero;
    }

    private static string CommandLineOf(IntPtr handle)
    {
        // Asked with no buffer, the call answers "length mismatch" and says how long the buffer must be.
        int probe = NtQueryInformationProcess(handle, ProcessCommandLineInformationClass, IntPtr.Zero, 0, out int length);
        if (probe != StatusInfoLengthMismatch || length <= 0)
        {
            return string.Empty;
        }

        IntPtr buffer = Marshal.AllocHGlobal(length);
        try
        {
            int status = NtQueryInformationProcess(handle, ProcessCommandLineInformationClass, buffer, length, out _);
            UnicodeString text = Marshal.PtrToStructure<UnicodeString>(buffer);

            return status != 0 || text.Buffer == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(text.Buffer, text.Length / 2);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Runs a query against a live process; a process that has gone, or refuses the query, reads as the default (null for the nullable results used here).</summary>
    private static T WithProcess<T>(int processId, Func<Process, T> query)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            return query(process);
        }
        catch (ArgumentException)
        {
            return default!;
        }
        catch (InvalidOperationException)
        {
            return default!;
        }
        catch (Win32Exception)
        {
            // Protected processes refuse the query; a reused identifier may land on one.
            return default!;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessBasicInformation
    {
        public IntPtr Reserved1;
        public IntPtr PebBaseAddress;
        public IntPtr Reserved2a;
        public IntPtr Reserved2b;
        public IntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public UIntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int PriorityClassBase;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCountersEx2
    {
        public uint Size;
        public uint PageFaultCount;
        public UIntPtr PeakWorkingSetSize;
        public UIntPtr WorkingSetSize;
        public UIntPtr QuotaPeakPagedPoolUsage;
        public UIntPtr QuotaPagedPoolUsage;
        public UIntPtr QuotaPeakNonPagedPoolUsage;
        public UIntPtr QuotaNonPagedPoolUsage;
        public UIntPtr PagefileUsage;
        public UIntPtr PeakPagefileUsage;
        public UIntPtr PrivateUsage;
        public UIntPtr PrivateWorkingSetSize;
        public ulong SharedCommitUsage;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, out ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr processHandle, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(IntPtr processHandle, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32FirstW(IntPtr snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32NextW(IntPtr snapshot, ref ProcessEntry entry);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(IntPtr processHandle, ref ProcessMemoryCountersEx2 counters, uint size);
}