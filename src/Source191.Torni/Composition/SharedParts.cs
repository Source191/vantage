using Source191.Torni.Adapters.Diagnostics;
using Source191.Torni.Adapters.Files;
using Source191.Torni.Adapters.Processes;
using Source191.Torni.Adapters.State;
using Source191.Torni.Adapters.Terminal;
using Source191.Torni.Application.Diagnostics;
using Source191.Torni.Domain;
using System.Reflection;

namespace Source191.Torni.Composition;

/// <summary>What more than one command uses: the state store, the processes, the versions, the log and the crash recorder.</summary>
internal sealed record SharedParts(
    JsonFileStore Store,
    ProcessRegistry ProcessRegistry,
    ToolVersion Version,

    string TorniExecutable,
    string OwnFolder,
    TerminalFragment TerminalFragment,
    ProgramLocator Programs,
    DiagnosticLogSetting LogSetting,
    DiagnosticLog Log,
    PerformanceCounts Counts,
    RecordedProcessRunner Processes,
    CrashFile CrashFile,
    CrashRecorder Crashes)
{
    public static SharedParts ForCurrentUser()
    {
        string stateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Torni");
        JsonFileStore store = new(stateFolder, TimeProvider.System);
        ProcessRegistry processRegistry = new();
        ToolVersion version = ToolVersion.FromInformational(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty);

        string torniExecutable = Environment.ProcessPath ?? "torni";
        string ownFolder = ProgramFolder.Of(AppContext.BaseDirectory, Environment.ProcessPath);
        TerminalFragment terminalFragment = TerminalFragment.ForCurrentUser(Path.Combine(ownFolder, "logo.ico"));

        // Every program Torni starts is found on the PATH once and run by its full path, never by a bare name that Windows
        // would look for in the folder Torni was started from first
        // (https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw).
        ProgramLocator programs = ProgramLocator.ForCurrentUser();

        // The diagnostic log is always kept in memory; TORNI_LOG=<file> also writes it to that file.
        DiagnosticLogSetting logSetting = DiagnosticLogSetting.Read(Environment.GetEnvironmentVariable("TORNI_LOG"));
        DiagnosticLog log = new(TimeProvider.System, logSetting is DiagnosticLogSetting.File logFile ? [new DiagnosticFile(logFile.Path)] : []);
        ProcessCounts processCounts = new();
        PerformanceCounts counts = new(processCounts, new RefreshCounts(TimeProvider.System, log), new StallCounts());
        // How long a captured run may take: a claude agents listing takes about two seconds, the slowest gh or az call a few.
        TimeSpan captureLimit = TimeSpan.FromSeconds(60);
        RecordedProcessRunner processes = new(new ProcessRunner(captureLimit), log, processCounts, TimeProvider.System);
        CrashFile crashFile = new(stateFolder);
        CrashRecorder crashes = new(log, counts, crashFile, TimeProvider.System);

        return new SharedParts(store, processRegistry, version, torniExecutable, ownFolder, terminalFragment, programs, logSetting, log, counts, processes, crashFile, crashes);
    }

    // The start facts: versions and the terminal's size, never a path or a name.
    public Started RecordStart(ClaudeVersionReport claudeVersionReport)
    {
        Started start = Started.From(Version, claudeVersionReport, Environment.OSVersion.Version, Environment.Version, Console.WindowWidth, Console.WindowHeight);
        Log.Record(start);

        return start;
    }
}