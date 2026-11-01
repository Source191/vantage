using Source191.Vantage.Domain;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Reads process start times, parents, the whole table and what a process uses from the operating system (times in
/// UTC so a value survives a JSON round trip unchanged), and ends processes either with their whole tree or cleanly
/// with exit code 0.
/// </summary>
public sealed partial class ProcessRegistry : IProcessRegistry
{
    private const uint SnapshotOfProcesses = 0x2;
    private const int ProcessBasicInformationClass = 0;
    private const int ProcessCommandLineInformationClass = 60;
    private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
    private const uint ProcessQueryLimitedInformation = 0x1000;

    public DateTimeOffset? StartTimeOf(int processId) => WithProcess<DateTimeOffset?>(processId, process => new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero), whenGone: null);

    public void Terminate(int processId)
    {
        WithProcess(processId, process =>
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(TimeSpan.FromSeconds(5));
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
                : WithProcess<ProcessParent?>((int)parentId, parent => new ProcessParent((int)parentId, parent.ProcessName), whenGone: null);
        }, whenGone: null);
    }

    public void EndCleanly(int processId)
    {
        WithProcess(processId, process =>
        {
            if (TerminateProcess(process.Handle, 0))
            {
                process.WaitForExit(TimeSpan.FromSeconds(5));
            }
        });
    }

    /// <summary>
    /// One <see href="https://learn.microsoft.com/windows/win32/api/tlhelp32/nf-tlhelp32-createtoolhelp32snapshot">Toolhelp
    /// snapshot</see>: every process with its parent's id, no handles opened, a few milliseconds for hundreds of processes.
    /// </summary>
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
    /// The <see href="https://learn.microsoft.com/windows/win32/api/psapi/ns-psapi-process_memory_counters_ex2">private
    /// working set</see> (Task Manager's Memory column, shared pages excluded), the
    /// <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes">start
    /// time</see> and the command line, from one
    /// <see href="https://learn.microsoft.com/windows/win32/procthread/process-security-and-access-rights">limited-rights
    /// handle</see> <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess">opened
    /// directly</see>: <see cref="Process.GetProcessById(int)"/> walks the whole
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
            // A process that has ended but is still held open has an exit time; a running one has none
            // (https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes).
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
            entries.Add(new ProcessFacts((int)entry.ProcessId, (int)entry.ParentProcessId, Path.GetFileNameWithoutExtension(entry.ExeFile.ToString())));
            more = Process32NextW(snapshot, ref entry);
        }

        return entries;
    }

    /// <summary>
    /// The private working set when the
    /// <see href="https://learn.microsoft.com/windows/win32/api/psapi/nf-psapi-getprocessmemoryinfo">operating system
    /// reports it</see> (<see href="https://learn.microsoft.com/windows/win32/api/psapi/ns-psapi-process_memory_counters_ex2">Windows
    /// 10 2004 onwards</see>), else the working set from the older structure, else nothing.
    /// </summary>
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
        // Asked with no buffer, the call answers "length mismatch" and says how long the buffer must be
        // (https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess).
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

    /// <summary>Runs a query against a live process; a process that has gone, or refuses the query, reads as <paramref name="whenGone"/>.</summary>
    private static T WithProcess<T>(int processId, Func<Process, T> query, T whenGone)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            return query(process);
        }
        catch (ArgumentException)
        {
            return whenGone;
        }
        catch (InvalidOperationException)
        {
            return whenGone;
        }
        catch (Win32Exception)
        {
            // Protected processes refuse the query; a reused identifier may land on one.
            return whenGone;
        }
    }

    /// <summary>Acts on a live process; a process that has gone, or refuses the action, is left alone.</summary>
    private static void WithProcess(int processId, Action<Process> action)
    {
        try
        {
            using Process process = Process.GetProcessById(processId);

            action(process);
        }
        catch (ArgumentException)
        {
            // The process has gone.
        }
        catch (InvalidOperationException)
        {
            // The process exited while it was being acted on.
        }
        catch (Win32Exception)
        {
            // Protected processes refuse the action; a reused identifier may land on one.
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

    [StructLayout(LayoutKind.Sequential)]
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

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, out ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    [LibraryImport("ntdll.dll")]
    private static partial int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, IntPtr processInformation, int processInformationLength, out int returnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateProcess(IntPtr processHandle, uint exitCode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr CreateToolhelp32Snapshot(uint flags, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessTimes(IntPtr processHandle, out long creationTime, out long exitTime, out long kernelTime, out long userTime);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Process32FirstW(IntPtr snapshot, ref ProcessEntry entry);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Process32NextW(IntPtr snapshot, ref ProcessEntry entry);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr handle);

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetProcessMemoryInfo(IntPtr processHandle, ref ProcessMemoryCountersEx2 counters, uint size);
}