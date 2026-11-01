using Source191.Vantage.Adapters.Diagnostics;
using Source191.Vantage.Adapters.Processes;
using Source191.Vantage.Adapters.State;
using Source191.Vantage.Adapters.Terminal;
using Source191.Vantage.Application.Diagnostics;
using Source191.Vantage.Domain;
using System.Reflection;

namespace Source191.Vantage.Composition;

/// <summary>What more than one command uses: the state store, the processes, the versions, the log and the crash recorder.</summary>
internal sealed record SharedParts(
    JsonFileStore Store,
    ProcessRegistry ProcessRegistry,
    ToolVersion Version,
    string VantageExecutable,
    TerminalFragment TerminalFragment,
    ProgramLocator Programs,
    DiagnosticLogSetting LogSetting,
    DiagnosticLog Log,
    ProcessCounts ProcessCounts,
    RecordedProcessRunner Processes,
    CrashFile CrashFile,
    CrashRecorder Crashes)
{
    public static SharedParts ForCurrentUser()
    {
        string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vantage");
        JsonFileStore store = new(stateFolder, TimeProvider.System);
        ProcessRegistry processRegistry = new();
        ToolVersion version = ToolVersion.FromInformational(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty);
        string vantageExecutable = Environment.ProcessPath ?? "vantage";
        TerminalFragment terminalFragment = TerminalFragment.ForCurrentUser(Path.Combine(AppContext.BaseDirectory, "vantage.ico"));

        // Every program Vantage starts is found on the PATH once and run by its full path, never by a bare name that Windows
        // would look for in the folder Vantage was started from first
        // (https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw).
        ProgramLocator programs = ProgramLocator.ForCurrentUser();

        // The diagnostic log is always kept in memory; VANTAGE_LOG=<file> also writes it to that file.
        DiagnosticLogSetting logSetting = DiagnosticLogSetting.Read(Environment.GetEnvironmentVariable("VANTAGE_LOG"));
        DiagnosticLog log = new(TimeProvider.System, logSetting is DiagnosticLogSetting.File logFile ? [new DiagnosticFile(logFile.Path)] : []);
        ProcessCounts processCounts = new();
        RecordedProcessRunner processes = new(new ProcessRunner(), log, processCounts, TimeProvider.System);
        CrashFile crashFile = new(stateFolder);
        CrashRecorder crashes = new(log, processCounts, crashFile);

        return new SharedParts(store, processRegistry, version, vantageExecutable, terminalFragment, programs, logSetting, log, processCounts, processes, crashFile, crashes);
    }

    // The start facts: versions and the terminal's size, never a path or a name.
    public Started RecordStart(ClaudeVersionReport claudeVersionReport)
    {
        Started start = Started.From(Version, claudeVersionReport, Environment.OSVersion.Version, Environment.Version, Console.WindowWidth, Console.WindowHeight);
        Log.Record(start);

        return start;
    }
}