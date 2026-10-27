using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Source191.Vantage.HeadlessTerminal;

/// <summary>A child process attached to a ConPTY pseudo-console: its input pipe, its output pipe and its exit state.</summary>
[SupportedOSPlatform("windows")]
public sealed class PseudoConsole : IDisposable
{
    private readonly IntPtr pseudoConsole;
    private readonly IntPtr process;
    private readonly IntPtr thread;
    private readonly IntPtr attributeList;
    private readonly FileStream input;
    private bool closed;

    private PseudoConsole(IntPtr pseudoConsole, NativeMethods.ProcessInformation information, IntPtr attributeList, FileStream input, FileStream output)
    {
        this.pseudoConsole = pseudoConsole;
        process = information.Process;
        thread = information.Thread;
        this.attributeList = attributeList;
        this.input = input;
        Output = output;
    }

    public FileStream Output { get; }

    public bool HasExited => NativeMethods.WaitForSingleObject(process, 0) == NativeMethods.WaitObject0;

    public int ExitCode => NativeMethods.GetExitCodeProcess(process, out uint code) ? (int)code : -1;

    public static PseudoConsole Start(string exe, IReadOnlyList<string> arguments, string workingDirectory, int columns, int rows, IReadOnlyDictionary<string, string> environment)
    {
        (IntPtr inputRead, IntPtr inputWrite) = Pipe();
        (IntPtr outputRead, IntPtr outputWrite) = Pipe();
        NativeMethods.Coord size = new() { X = (short)columns, Y = (short)rows };
        int created = NativeMethods.CreatePseudoConsole(size, inputRead, outputWrite, 0, out IntPtr pseudoConsole);
        if (created != 0)
        {
            throw new InvalidOperationException($"CreatePseudoConsole failed with 0x{created:X8}");
        }

        // The pseudo-console duplicated its ends of the pipes; the originals must go so reads end when it closes.
        NativeMethods.CloseHandle(inputRead);
        NativeMethods.CloseHandle(outputWrite);

        IntPtr attributeList = AttributeListFor(pseudoConsole);
        NativeMethods.StartupInfoEx startup = new()
        {
            // UseStandardHandles with null handles: without it a child inherits the driver's own stdout when that is piped, and its output bypasses the pseudo-console.
            StartupInfo = new NativeMethods.StartupInfo { Size = Marshal.SizeOf<NativeMethods.StartupInfoEx>(), Flags = NativeMethods.UseStandardHandles },
            AttributeList = attributeList,
        };
        string commandLine = CommandLine.Build(exe, arguments);
        IntPtr environmentBlock = Marshal.StringToHGlobalUni(EnvironmentBlock(environment));
        try
        {
            uint flags = NativeMethods.ExtendedStartupInfoPresent | NativeMethods.CreateUnicodeEnvironment;
            if (!NativeMethods.CreateProcess(null, commandLine, IntPtr.Zero, IntPtr.Zero, false, flags, environmentBlock, workingDirectory, ref startup, out NativeMethods.ProcessInformation information))
            {
                throw new InvalidOperationException($"CreateProcess failed with error {Marshal.GetLastWin32Error()} for {commandLine}");
            }

            FileStream input = new(new SafeFileHandle(inputWrite, ownsHandle: true), FileAccess.Write, 1, isAsync: false);
            FileStream output = new(new SafeFileHandle(outputRead, ownsHandle: true), FileAccess.Read, 16384, isAsync: false);

            return new PseudoConsole(pseudoConsole, information, attributeList, input, output);
        }
        finally
        {
            Marshal.FreeHGlobal(environmentBlock);
        }
    }

    public async Task WriteAsync(string text)
    {
        await input.WriteAsync(Encoding.UTF8.GetBytes(text));
        await input.FlushAsync();
    }

    public void Kill()
    {
        if (NativeMethods.TerminateProcess(process, 1))
        {
            _ = NativeMethods.WaitForSingleObject(process, 2000);
        }
    }

    /// <summary>Closes the pseudo-console, which ends the output stream; the process handles stay until Dispose.</summary>
    public void Close()
    {
        if (!closed)
        {
            closed = true;
            NativeMethods.ClosePseudoConsole(pseudoConsole);
        }
    }

    public void Dispose()
    {
        Close();
        input.Dispose();
        Output.Dispose();
        NativeMethods.DeleteProcThreadAttributeList(attributeList);
        Marshal.FreeHGlobal(attributeList);
        NativeMethods.CloseHandle(thread);
        NativeMethods.CloseHandle(process);
    }

    private static (IntPtr Read, IntPtr Write) Pipe()
    {
        if (!NativeMethods.CreatePipe(out IntPtr read, out IntPtr write, IntPtr.Zero, 0))
        {
            throw new InvalidOperationException($"CreatePipe failed with error {Marshal.GetLastWin32Error()}");
        }

        return (read, write);
    }

    private static IntPtr AttributeListFor(IntPtr pseudoConsole)
    {
        IntPtr size = IntPtr.Zero;
        NativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
        IntPtr attributeList = Marshal.AllocHGlobal(size);
        bool ready = NativeMethods.InitializeProcThreadAttributeList(attributeList, 1, 0, ref size)
            && NativeMethods.UpdateProcThreadAttribute(attributeList, 0, NativeMethods.ProcThreadAttributePseudoConsole, pseudoConsole, IntPtr.Size, IntPtr.Zero, IntPtr.Zero);
        if (!ready)
        {
            throw new InvalidOperationException($"The pseudo-console attribute could not be set: error {Marshal.GetLastWin32Error()}");
        }

        return attributeList;
    }

    private static string EnvironmentBlock(IReadOnlyDictionary<string, string> environment)
    {
        StringBuilder block = new();
        foreach (KeyValuePair<string, string> pair in environment.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            block.Append(pair.Key).Append('=').Append(pair.Value).Append('\0');
        }

        return block.Append('\0').ToString();
    }
}