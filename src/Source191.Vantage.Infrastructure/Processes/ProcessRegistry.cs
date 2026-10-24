using Source191.Vantage.Domain;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Source191.Vantage.Adapters.Processes;

/// <summary>
/// Reads process start times and parents from the operating system (start times in UTC so the value survives a JSON
/// round trip unchanged), and ends processes either with their whole tree or cleanly with exit code 0.
/// </summary>
public sealed class ProcessRegistry : IProcessRegistry
{
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
            int status = NtQueryInformationProcess(process.Handle, 0, out ProcessBasicInformation information, Marshal.SizeOf<ProcessBasicInformation>(), out _);
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

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, out ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TerminateProcess(IntPtr processHandle, uint exitCode);
}