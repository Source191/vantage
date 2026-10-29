# Tasks

## 1. The values an event may hold (Application\Diagnostics)

- [x] 1.1 Red: `CodeNameShould.NameAnEffectType` asserts `CodeName.For(typeof(AttachToSession))` renders `AttachToSession` and a nested outcome type renders `StartSessionOutcome.Started`. Green: `CodeName` with only the `Type` and `MethodBase` factories. Verify the Core suite passes.
- [x] 1.2 Red: `ExceptionSummaryShould.KeepTypesAndFramesButNoMessage` throws an `InvalidOperationException` whose message and `Data` name `C:\Users\me\source\repos\Northwind.Orders`, wrapping a `GitException`, and asserts the summary names both types and the throwing method, and the rendered text contains neither the message nor `C:\Users\me`. Green: `ExceptionSummary.From`. Verify the fact passes.
- [x] 1.3 Red: `ExceptionSummaryShould.ShowTheSourceFileWithoutItsFolder` asserts a frame with a file path renders `GitCli.cs:145` and never the folder, and `ShouldCapFramesAndDepth` asserts at most 20 frames and three inner exceptions. Green: the frame rendering and caps. Verify the facts pass.
- [x] 1.4 Red: `ClaudeVersionShould.ReadTheLeadingNumber` asserts `2.1.310` parses to `Version(2, 1, 310)` and `unknown` does not. Green: `ClaudeVersion.TryParse`. Verify the fact passes.

## 2. Events, rendering and the log (Application\Diagnostics)

- [x] 2.1 Red: `DiagnosticLineShould` asserts one rendered line per event case (`Started`, `ViewShown`, `DialogShown`, `EffectRan`, `HandingOver`, `ChildReturned`, `ProcessSlow`, `ProcessFailedToStart`, `Failed`, `TerminalGuiReported`), each starting with the UTC time in `yyyy-MM-ddTHH:mm:ss.fffZ`. Green: the `DiagnosticEvent` records, `ViewKind`, `FailureSite` and `DiagnosticLine.Render`. Verify the facts pass.
- [x] 2.2 Red: `Architecture\DiagnosticEventsShould.HoldOnlyValuesVantageDefines` reflects over every concrete `DiagnosticEvent` and fails on a member outside `int`, `long`, `bool`, `TimeSpan`, `Version`, `ToolVersion`, enums, `CodeName`, `ExceptionSummary` and their nullable forms; `ExposeNoStringFactory` fails on a public method or constructor in the namespace taking a `string`. Prove both by adding a temporary `string` member and seeing them fail, then remove it. Green: no change if 2.1 obeyed. Verify the facts pass.
- [x] 2.3 Red: `DiagnosticLogShould.KeepTheLatest500` records 501 events and asserts the snapshot holds the last 500 in order; `WriteEachLineToEverySink` asserts a `RecordingSink` receives exactly the rendered snapshot line; `AcceptEventsFromManyThreads` records from 8 tasks and asserts the count. Green: `DiagnosticLog` (a concrete class: tests use the real one, so no `IDiagnostics` interface), `Ports\IDiagnosticSink`; `WriteCounts` writes the rendered counts to the sinks without keeping them. Verify the facts pass.
- [x] 2.4 Add `RecordingSink` to `test\Source191.Vantage.TestDoubles` (the sink is the boundary; the log itself is used for real, so no `RecordingDiagnostics`). Verify the solution builds.

## 3. Child programs (Application\Diagnostics)

- [x] 3.1 Red: `ChildCommandShould` asserts `git worktree list --porcelain` gives git `worktree list`, `git branch -d fix-123` gives git `branch`, `claude --bg -n name` gives claude `--bg`, `claude --bg --resume <id> -n name` gives claude `--bg --resume`, and `C:\Users\me\tools\MyTool.exe C:\Users\me\source\repos\Northwind.Orders` gives another program with no subcommand. Green: `ChildProgram`, `ChildCommandWords` and the subcommand value. Verify the facts pass.
- [x] 3.2 Red: `ProcessCountsShould.CountRunsByProgramSubcommandAndExitCode` adds three `worktree list` exit 0 runs and one `worktree remove` exit 128 and asserts the rendered counts with the longest durations. Green: `ProcessCounts`. Verify the fact passes.
- [x] 3.3 Red: `RecordedProcessRunnerShould` over `FakeProcessRunner` and a `FakeTimeProvider`: a fast run is counted and records no event; a 3.2 s run records `ProcessSlow` with 3200 ms; a run that throws records `ProcessFailedToStart` and rethrows; `RunInteractiveAsync` is counted but never called slow (it is a hand-off). Green: `RecordedProcessRunner`. Verify the facts pass.

## 4. Where Vantage records (Application)

- [ ] 4.1 Red: `EffectDispatcherShould.RecordEachEffectThatRuns` asserts a dispatched stop records `EffectRan(StopSession, StopSessionOutcome.…, duration)` and a handler that throws records nothing and still throws. Green: `EffectDispatcher` takes `IDiagnostics` and a `TimeProvider`; update `Composition`. Verify `EffectDispatcherShould` passes.
- [ ] 4.2 Red: `AttemptShould` asserts `Attempt.RunAsync` returns the action's result, and for a throwing action returns its message for the footer and records `Failed(site, what, summary)` once. Green: `Attempt`. Verify the facts pass.
- [ ] 4.3 Red: `ScreenLoopShould` (replacing its `RecordingLogger`) asserts `ViewShown(Sessions)` at start, `ViewShown(Worktrees)` on `w` and none on the re-run after an attach, `HandingOver(AttachToSession)` then `ChildReturned` around an attach with no job id or name in any line, and a failing update check recording `Failed(UpdateCheck, …)`. Green: `ScreenLoop` takes `IDiagnostics`; delete `HandoffLog` and `HandoffLogShould`. Verify `ScreenLoopShould` passes.
- [ ] 4.4 Red: `CrashRecorderShould.WriteTheLogCountsAndCrashThenRethrow` guards an action that throws and asserts `FakeCrashFile` received the rendered snapshot, the counts and the crash summary, and the same exception escapes; `LeaveTheCrashFileOnANormalReturn` asserts no write and the counts reach the sinks; `SurviveACrashFileThatCannotBeWritten` asserts the original exception still escapes. Green: `CrashRecorder`, `Ports\ICrashFile`, `FakeCrashFile` in TestDoubles. Verify the facts pass.

## 5. Adapters

- [ ] 5.1 Red: `DiagnosticFileShould` (Infrastructure, temporary folder) asserts two writes append two UTF-8 lines without a BOM and a write to a locked file does not throw. Green: `Adapters\Diagnostics\DiagnosticFile`; delete `FileLogger`. Verify the Infrastructure suite passes.
- [ ] 5.2 Red: `TerminalGuiBridgeShould` asserts a Warning whose message quotes `C:\Users\me` and an input sequence records `TerminalGuiReported(Warning, null)` with neither in the rendered line, an Error with an exception carries its summary, and `IsEnabled` is false for Trace, Debug and Information. Green: `Adapters\Diagnostics\TerminalGuiBridge`. Verify the facts pass.
- [ ] 5.3 Red: `CrashFileShould` (temporary folder as the state folder) asserts a write creates `last-crash.log` and a second replaces it whole. Green: `Adapters\Diagnostics\CrashFile`. Verify the facts pass.

## 6. Screens and wiring (Tui, Program.cs)

- [ ] 6.1 `TableScreen.TriggerLoad` and `RunInBackground` call `Attempt.RunAsync` (`FailureSite.Load` with the view's `ViewKind`, `FailureSite.Effect` with the working effect's `CodeName`) instead of their own `catch`; `DialogTabTitle.Apply` records `DialogShown`. Verify the Core suite passes and a headless `drive` that opens Settings and makes a load fail (a root that is a file) shows the same footer as before and a `DialogShown(Settings)` and `Failed(Load, Sessions, …)` line in a `VANTAGE_LOG` file.
- [ ] 6.2 `Program.cs`: one `DiagnosticLog` with a `DiagnosticFile` sink when `VANTAGE_LOG` is set; `RecordedProcessRunner` around the one `ProcessRunner`; `TerminalGuiBridge` always assigned; `Started` recorded at the console action's start; `CrashRecorder.Guard` around the console and `here` actions; `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` subscribed. Verify `dotnet build -c Debug` and that a local install started with `VANTAGE_LOG` writes the start line with the four versions and the terminal size.
- [ ] 6.3 Check whether the packed nupkg carries portable PDBs (`Expand-Archive` the CI nupkg and look for `Source191.Vantage.Core.pdb` beside the dll). Verify by recording the answer in design.md's risk; if they are missing, frames render without a line and the spec's "where known" holds.

## 7. The privacy guarantee

- [ ] 7.1 Red: `DiagnosticsPrivacyShould.RecordNothingFromTheFixtures` composes the real reducers, dispatcher, handlers, `RecordedProcessRunner`, `DiagnosticLog` and `CrashRecorder` over the TestDoubles fakes seeded with Northwind fixtures and drives a sessions load, an attach, a wizard start with a name and prompt, a worktree remove whose fake git fails with stderr naming the folders, a failing load and a crash; it then asserts no seeded value (names, branches, ids, prompts, folders, PR numbers and titles, `C:\Users\me`) appears in any rendered line, the counts or the crash text. The value list is collected from the seeds. Green: fix whatever leaks. Verify the fact passes, and fails when `HandingOver` is temporarily given `effect.ToString()`.

## 8. Docs

- [ ] 8.1 README: Troubleshooting's `VANTAGE_LOG` item says what the file holds; "Where Vantage keeps things" lists `last-crash.log`; a short "What Vantage records" paragraph lists the recorded facts and the never-recorded ones from the spec. CLAUDE.md: Core's packages (Logging.Abstractions leaves), the diagnostic lines as the one text outside `Wording`, and a line that every new diagnostic fact is a typed event case covered by `DiagnosticEventsShould`. Verify `grep -n "VANTAGE_LOG" README.md CLAUDE.md` reads consistently with the spec.

## 9. Version and checks

- [ ] 9.1 Raise `Version` one preview step above `main`'s (a minor raise). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass.
- [ ] 9.2 Run `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` and the `smoke` check against a locally packed build. Verify exit 0 for both.
- [ ] 9.3 The product owner installs the build with `tools\install-local.ps1`, runs with `VANTAGE_LOG` set, attaches, starts a session with a prompt, removes a dirty worktree (refused), then reads the file. Verify no name, folder, branch, id or prompt appears and the counts are written on `q`; record the result here.

## Workflow follow-up

- Archive the change in the same pull request, syncing the `diagnostics` and `tool-updates` deltas into `openspec\specs`.
- `report-a-problem` builds on this change; merge this one first.
