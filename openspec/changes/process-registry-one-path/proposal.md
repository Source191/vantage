# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026, sections 3.2, 3.3, 3.4 and 3.8, items I-05, I-13, I-04 and D-19) found that `src\Source191.Vantage.Infrastructure\Processes\ProcessRegistry.cs` reads the same fact two ways: `UsageOf` opens a limited handle and calls `GetProcessTimes` because `Process.GetProcessById` was measured at 20 ms, while `StartTimeOf` (`ProcessRegistry.cs:22`) and `ParentOf` (`:33-44`) still walk the table through `GetProcessById`, once per claimed tab per refresh and three times per step of `TerminalTab.Walk`, and `ParentOf` needs the full-access `Process.Handle` the limited path was introduced to avoid. The same review found two security rules held by discipline rather than shape: `IProcessRegistry.Terminate(int)` and `EndCleanly(int)` will end any id and only the callers remember to check `ProcessIdentity.StillTheSession` first, and `ProcessUsage.CommandLine` keeps a whole command line, which may carry secrets, inside every `ProcessTree` node and prints it through the record's `ToString()`.

## What Changes

- **One limited-handle path (I-05):** a private `Opened(processId, Func<nint, T>)` around `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION)` is the one way the adapter reads a process. `StartTimeOf` reads `GetProcessTimes` through it (the body `UsageOf` already has), `ParentOf` reads `NtQueryInformationProcess(ProcessBasicInformation)` through it and names the parent with `QueryFullProcessImageNameW`, and `UsageOf` keeps its handle. `Process.GetProcessById` remains only where a kill needs a `Process`: `Terminate` and `EndCleanly`.
- **The interop in its own file (I-13):** the structs and `[LibraryImport]` declarations move to a `ProcessRegistryNative.cs` partial, `nint`/`nuint` replace `IntPtr`/`UIntPtr` as `Terminal\ConsoleRecovery.cs` already does, and one `WithProcess` remains.
- **`ConfirmedProcess` (I-04):** a Domain record with a private constructor that only its own `TryConfirm` doors mint: the listing's rule (started no later than the session plus five seconds, today `ProcessIdentity.StillTheSession`), the tab claim's rule (the recorded start time exactly, today inside `TabRegistration.IsLive`) and the walk's rule (a parent started no later than its confirmed child, today inside `TerminalTab.Walk`). `IProcessRegistry.Terminate` and `EndCleanly` take a `ConfirmedProcess`, `TerminalTab.ShellOf` answers one from a confirmed root, `TabRegistration.TryConfirmShell` answers a live claimed tab's shell, and `ProcessIdentity` is retired into the new type. A handler can no longer end an id it has not checked; `AdoptSessionHandler`, `CloseEmptyTerminalsHandler`, `SessionTabs` and `FakeProcessRegistry` change to match.
- **The command line never leaves the adapter (D-19):** `ProcessUsage` carries a `Label` (`ProgramLabel.From(name, commandLine)` applied in `UsageOf`) instead of the command line, so nothing outside the adapter's one method ever holds it; `MemoryBreakdown` reads the label, and `FakeProcessRegistry.Using` takes a label.
- **Docs:** `CLAUDE.md`'s and `.claude\rules\security.md`'s identity and command-line bullets name the new shapes, both rule files' path globs follow `ProcessIdentity.cs` to `ConfirmedProcess.cs`, and `.claude\rules\windows.md` records the newly verified process facts with their date and version.
- **Version:** raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above `main` when implemented (0.20.0-preview.9 if nothing lands first); a refactor, so patch-level under semantic versioning.
- `skip_specs: true`: no key, screen, message, state file or command line behaves differently; the integration facts in `ProcessRegistryIntegrationShould` pin the answers the adapter gives.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with tests and documentation, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Core\Domain` gains `ConfirmedProcess.cs` and loses `ProcessIdentity.cs`; `IProcessRegistry.cs`, `TabRegistration.cs`, `TerminalTab.cs` and `ProcessUsage.cs` change; `Application\MemoryBreakdown.cs`, `Actions\AdoptSessionHandler.cs`, `Actions\CloseEmptyTerminalsHandler.cs` and `Actions\SessionTabs.cs` change; `src\Source191.Vantage.Infrastructure\Processes\ProcessRegistry.cs` is split with `ProcessRegistryNative.cs`. No Tui type, effect, outcome, `Wording` entry or `Program.cs` line changes (`Program.cs:152` calls `UsageOf`, whose signature stays).
- **Tests:** `ProcessIdentityShould` becomes `ConfirmedProcessShould`; `TabRegistrationShould`, `TerminalTabShould`, `MemoryBreakdownShould`, `NullFreeModelsShould.Kept` and `test\Source191.Vantage.TestDoubles\FakeProcessRegistry.cs` change; `ProcessRegistryIntegrationShould` gains a start-time timing fact and a real-process label fact. The handler tests keep asserting `Terminated` and `EndedCleanly` as ids.
- **Other open changes:** `fix-removal-paths` should land first: it adds the `AggregateException` catch to `WithProcess` (C-08), which the one surviving `WithProcess` keeps, and `TerminalTab.ShellAbove` with `EndCleanly` on claude when claude is the tab's root (A-05), which this change retypes over `ConfirmedProcess`. `fix-refresh-ordering` touches `Processes\ProcessRunner.cs` in the same folder; whichever lands second rebases. If `folder-deleter-and-ports` moves `IProcessRegistry` to `Ports` (D-09) first, the interface file moves and this rebases; `ConfirmedProcess` stays in Domain either way.
- **Users:** none; a Sessions refresh with claimed tabs and Enter on a row open fewer table walks, which is a few hundred milliseconds saved at ten tabs.

## Out of scope

- Moving `IProcessRegistry` from Domain to Ports (D-09): another change's work.
- The fake's fidelity (`FakeProcessRegistry.Terminate` leaving children running, `Snapshot` omitting `Running` ids, T-12): `test-hygiene`.
- A `ProcessId` value type for the bare `int` ids across ten types, or a `ProgramLabel` value type for the label: the ints come straight from the OS and the label is read by one caller.
- Ending a process through a limited handle (`OpenProcess(PROCESS_TERMINATE)`): `Kill(entireProcessTree: true)` needs a `Process`, so the two kills keep `GetProcessById`.
- Multiple Claude accounts, which is a later change.
