using Source191.Torni.Domain;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Source191.Torni.Adapters.Processes;

/// <summary>
/// Reads process start times, parents, the whole table and what a process uses from the operating system (times in
/// UTC so a value survives a JSON round trip unchanged), and ends processes either with their whole tree or cleanly
/// with exit code 0. Every read opens its own
/// <see href="https://learn.microsoft.com/windows/win32/procthread/process-security-and-access-rights">limited-rights
/// handle</see> directly: <see cref="Process.GetProcessById(int)"/> walks the whole table on every call, about twenty
/// milliseconds, and a refresh reads a few hundred processes. It is used only to end a process, which needs a
/// <see cref="Process"/>.
/// </summary>
public sealed partial class ProcessRegistry : IProcessRegistry
{
    public DateTimeOffset? StartTimeOf(int processId) => Opened<DateTimeOffset?>(processId, StartOf, whenGone: null);

    public void Terminate(ConfirmedProcess process)
    {
        WithProcess(process.Id, running =>
        {
            running.Kill(entireProcessTree: true);
            running.WaitForExit(TimeSpan.FromSeconds(5));
        });
    }

    public ProcessParent? ParentOf(int processId) => Opened<ProcessParent?>(processId, ParentOfOpened, whenGone: null);

    public void EndCleanly(ConfirmedProcess process)
    {
        WithProcess(process.Id, running =>
        {
            if (TerminateProcess(running.Handle, 0))
            {
                running.WaitForExit(TimeSpan.FromSeconds(5));
            }
        });
    }

    /// <summary>
    /// One <see href="https://learn.microsoft.com/windows/win32/api/tlhelp32/nf-tlhelp32-createtoolhelp32snapshot">Toolhelp
    /// snapshot</see>: every process with its parent's id, no handles opened, a few milliseconds for hundreds of processes.
    /// </summary>
    public IReadOnlyList<ProcessFacts> Snapshot()
    {
        nint snapshot = CreateToolhelp32Snapshot(SnapshotOfProcesses, 0);
        if (snapshot == 0 || snapshot == -1)
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
    /// The private working set (Task Manager's Memory column, shared pages excluded), the start time and the program's
    /// label. The command line is read to make the label, which keeps only the program's short name, and is dropped
    /// here: it may carry secrets, so nothing outside this method ever holds it.
    /// </summary>
    public ProcessUsage? UsageOf(int processId) => Opened<ProcessUsage?>(processId, handle =>
    {
        DateTimeOffset? startedAt = StartOf(handle);

        return startedAt is DateTimeOffset started
            ? new ProcessUsage(started, PrivateMemoryOf(handle), LabelOf(handle))
            : null;
    }, whenGone: null);

    /// <summary>The label of the program behind the handle, or empty when its name cannot be read.</summary>
    private static string LabelOf(nint handle)
    {
        string name = NameOf(handle);

        return name.Length == 0 ? string.Empty : ProgramLabel.From(name, CommandLineOf(handle));
    }

    /// <summary>
    /// Opens the process with <see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocess">the
    /// rights to query limited information</see>, runs the query and closes the handle; a process that has gone, or
    /// refuses the handle, reads as <paramref name="whenGone"/>.
    /// </summary>
    private static T Opened<T>(int processId, Func<nint, T> query, T whenGone)
    {
        nint handle = OpenProcess(ProcessQueryLimitedInformation, false, (uint)processId);
        if (handle == 0)
        {
            return whenGone;
        }

        try
        {
            return query(handle);
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    /// <summary>
    /// When the process started, in UTC. A process that has ended but is still held open has an exit time and a running
    /// one has none
    /// (<see href="https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-getprocesstimes">GetProcessTimes</see>).
    /// </summary>
    private static DateTimeOffset? StartOf(nint handle) => GetProcessTimes(handle, out long creation, out long exit, out _, out _) && exit == 0
        ? new DateTimeOffset(DateTime.FromFileTimeUtc(creation), TimeSpan.Zero)
        : null;

    private static ProcessParent? ParentOfOpened(nint handle)
    {
        int status = NtQueryInformationProcess(handle, ProcessBasicInformationClass, out ProcessBasicInformation information, Marshal.SizeOf<ProcessBasicInformation>(), out _);
        long parentId = information.InheritedFromUniqueProcessId;
        if (status != 0 || parentId <= 0 || parentId > int.MaxValue)
        {
            return null;
        }

        return Opened<ProcessParent?>((int)parentId, parent => new ProcessParent((int)parentId, NameOf(parent)), whenGone: null);
    }

    /// <summary>The executable's name without its folder or extension, as <see cref="Snapshot"/> names it; empty when it cannot be read.</summary>
    private static string NameOf(nint handle)
    {
        char[] buffer = new char[1024];
        int length = buffer.Length;

        return QueryFullProcessImageNameW(handle, 0, buffer, ref length)
            ? Path.GetFileNameWithoutExtension(new string(buffer, 0, length))
            : string.Empty;
    }

    private static List<ProcessFacts> ReadEntries(nint snapshot)
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
    private static MemorySize PrivateMemoryOf(nint handle)
    {
        ProcessMemoryCountersEx2 counters = new() { Size = (uint)Marshal.SizeOf<ProcessMemoryCountersEx2>() };
        if (GetProcessMemoryInfo(handle, ref counters, counters.Size))
        {
            return MemorySize.FromBytes((long)counters.PrivateWorkingSetSize);
        }

        counters.Size = (uint)Marshal.OffsetOf<ProcessMemoryCountersEx2>(nameof(ProcessMemoryCountersEx2.PrivateWorkingSetSize)).ToInt32();

        return GetProcessMemoryInfo(handle, ref counters, counters.Size)
            ? MemorySize.FromBytes((long)counters.WorkingSetSize)
            : MemorySize.Zero;
    }

    private static string CommandLineOf(nint handle)
    {
        // Asked with no buffer, the call answers "length mismatch" and says how long the buffer must be
        // (https://learn.microsoft.com/windows/win32/api/winternl/nf-winternl-ntqueryinformationprocess).
        int probe = NtQueryInformationProcess(handle, ProcessCommandLineInformationClass, 0, 0, out int length);
        if (probe != StatusInfoLengthMismatch || length <= 0)
        {
            return string.Empty;
        }

        nint buffer = Marshal.AllocHGlobal(length);
        try
        {
            int status = NtQueryInformationProcess(handle, ProcessCommandLineInformationClass, buffer, length, out _);
            UnicodeString text = Marshal.PtrToStructure<UnicodeString>(buffer);

            return status != 0 || text.Buffer == 0 ? string.Empty : Marshal.PtrToStringUni(text.Buffer, text.Length / 2);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
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
        catch (AggregateException)
        {
            // Process.Kill(entireProcessTree: true) throws this when a descendant has already gone or cannot be ended; what could be ended was.
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
}