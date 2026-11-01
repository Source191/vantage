# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step; the test is named before the code. Groups 1 to 4 add tested `Application` classes that nothing uses yet; group 5 moves the wiring onto them. Verify with `dotnet build -c Debug` (warnings are errors) and `dotnet test --project test\Source191.Vantage.Core.Tests -c Debug --no-build` unless a task says otherwise. New facts use the fakes in `test\Source191.Vantage.TestDoubles` that the neighbouring facts already use, Northwind fixtures and `C:\Users\me` paths.

## 1. The required programs

- [x] 1.1 `RequiredProgramsShould.FindBothPrograms`: `RequiredPrograms.Check(new ClaudeLocation.Found(claude), new ProgramLocation.Found(git))` answers `Found` with both paths. Red (the type does not exist), then green with the abstract record, its nested `Found(string Claude, string Git)` and `Missing(string Sentence)`, and `Check`, in `src\Source191.Vantage.Core\Application\RequiredPrograms.cs`. Verify the fact passes and `NullFreeModelsShould` and `SealedTypesShould` stay green.
  Red was a build failure (no type), so it is folded into the green commit; Core.Tests 1957 passed, the architecture facts among them.
- [x] 1.2 `RequiredProgramsShould.RefuseABatchFile`, `SayClaudeCodeIsMissing` and `SayGitIsMissing`: `Missing` with `Wording.ClaudeIsABatchFile(path)`, `Wording.ClaudeNotFound` and `Wording.GitNotFound`. Red, then green. Verify the facts pass.
  Red: three facts failed on the naive casts; green: a switch naming every `ClaudeLocation` case, discard arm `UnreachableException`; Core.Tests 1960 passed.
- [x] 1.3 `RequiredProgramsShould.PutClaudeCodeFirstWhenBothAreMissing`: a missing Claude Code and a missing git answer Claude Code's sentence, as `Program.cs` does today. Verify the fact passes (green at once is acceptable; say so in the commit).
  Green at once: 1.2's switch already judges Claude Code first; Core.Tests 1961 passed.

## 2. The start event

- [x] 2.1 `StartedShould.CarryTheVersionClaudeCodePrinted`: `Started.From(version, new ClaudeVersionReport.Printed("2.1.310"), windows, runtime, 120, 36)` carries Claude Code 2.1.310 and the other five values unchanged. Red, then green with `Started.From` in `src\Source191.Vantage.Core\Application\Diagnostics\Started.cs`, reading the report as `RecordStart` does. Verify the fact passes.
  Red was a build failure (no `From`), folded into green; `From` calls `ClaudeVersion.TryParse` with the house caller shape `out Version? parsed`; Core.Tests 1962 passed.
- [x] 2.2 `StartedShould.CarryNoVersionWhenNoneWasRead` (a theory over `NotRead` and a `Printed` text `ClaudeVersion.TryParse` refuses). Verify the fact passes and `DiagnosticEventsShould` stays green.
  Green at once (2.1's `From` already answers null for both); Core.Tests 1964 passed, `DiagnosticEventsShould` among them.

## 3. The update check's switch and the settings row

- [ ] 3.1 `UpdateCheckSwitchShould.BeOnWhenTheSettingIsOnAndNothingOptsOut`, `BeOffWhenTheVariableOptsOut` and `BeOffWhenTheSettingIsOff`, over `CurrentSettings` loaded from `InMemoryState`. Red, then green with `UpdateCheckSwitch(CurrentSettings settings, string optOutVariable)` and `IsOn()` in `src\Source191.Vantage.Core\Application\UpdateCheckSwitch.cs`, judging the variable once with `UpdateCheckOptOut.Applies`. Verify the facts pass.
- [ ] 3.2 `UpdateCheckSwitchShould.FollowASaveWithoutARestart`: turning `checkForUpdates` off through `CurrentSettings` turns `IsOn` off. Verify the fact passes.
- [ ] 3.3 `HeaderSettingsRowShould.ShowTheVersionsAndVantagesMemory`: `Rows(width)` is one row equal to `Wording.SettingsRow(vantage, claude, updates.Status, memory)`, with the memory `FakeProcessRegistry` reports for the process id. Red, then green with `HeaderSettingsRow(ToolVersion, ClaudeCodeVersion, UpdateCheck, IProcessRegistry, int processId)` in `src\Source191.Vantage.Core\Application\HeaderSettingsRow.cs`. Verify the fact passes.
- [ ] 3.4 `HeaderSettingsRowShould.ShowZeroWhenTheProcessCannotBeMeasured` (`UsageOf` answers null) and `ShowTheUpdateStatusAsItIsNow` (the status read at each call). Verify the facts pass.

## 4. The console's loads

- [ ] 4.1 `ConsoleLoadsShould.RefreshTheSessionsWithTheirAutomatedRuns`: `LoadSessionsAsync` answers `RowsLoaded` with a row per listed session and the listing's automated runs measured, as `SessionRowEnricher.Measure` reports them. Red, then green with `ConsoleLoads(LatestSessions, SessionRowEnricher, WorktreeInventory, SetupFactsReader, CurrentSettings)` in `src\Source191.Vantage.Core\Application\ConsoleLoads.cs`. Verify the fact passes.
- [ ] 4.2 `ConsoleLoadsShould.ScanWithTheLatestListingsSessions`: after one `LoadSessionsAsync`, `ScanWorktreesAsync` answers `WorktreeRowsLoaded` with the listed session on its worktree's row, and the fake CLI saw one listing in all. Red, then green. Verify the fact passes.
- [ ] 4.3 `ConsoleLoadsShould.ReadTheConfigRowsWithTheSetupFacts` (`ReadConfigAsync` answers `ConfigRowsLoaded` equal to `ConfigRows.For(settings, facts)`) and `ShowTheConfigRowsAsReadingFirst` (`ConfigRowsWhileReading()` equals `ConfigRows.For(settings, SetupFacts.Reading)`, and follows a saved setting). Red, then green. Verify the facts pass.

## 5. The wiring

Every task here is a refactor of untested wiring: verify with the build, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes`, and run each step as a verbatim move before any tidying.

- [ ] 5.1 `Program.cs` uses the new classes in place: `RequiredPrograms.Check` replaces `LocateClaudeAsync` and `LocateGitAsync` (a switch naming both cases, the discard arm throwing `UnreachableException`), `Started.From` replaces `RecordStart`'s body, `UpdateCheckSwitch.IsOn`, `HeaderSettingsRow.Rows` and `ConsoleLoads`' method groups replace the lambdas and the nested local functions. Verify as above and that `Program.cs` holds no `string?`.
- [ ] 5.2 Move the shared start into `src\Source191.Vantage\Composition\SharedParts.cs` (`ForCurrentUser()`, `RecordStart`) and the preamble into `LaunchParts.cs` (`RunAsync` writing the sentence and answering 1 on `Missing`), verbatim. Verify as above.
- [ ] 5.3 Move `OpenConsoleAsync`, `StartHereAsync` and `SetUpTerminalAsync` verbatim into `ConsoleComposition`, `HereComposition` and `TerminalSetupComposition`; `Program.cs` keeps the logger, the crash hooks, the `ClaimTab` lambda and the command line, with the order of design.md. Verify as above and that `Program.cs` is about 40 lines.
- [ ] 5.4 Tidy `ConsoleComposition`: private static methods by concern (pull requests, the dispatcher and its ports, the dialogs, the screen) and named locals for every argument list over the line length; the version call still starts first and is awaited just before `RecordStart`. Verify as above.
- [ ] 5.5 Hand-off check: `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` exits 0, with NO_COLOR, CLAUDECODE, CLAUDE_PID and CLAUDE_CODE_* cleared in that shell. The smoke check is left to CI's `check` job, because run locally it acts on the owner's real repositories. Record the result here.
- [ ] 5.6 Docs say the tool project (`Program.cs` and its `Composition` folder) is the only composition root and that its classes are untested wiring: `CLAUDE.md` (the layers paragraph and the TDD rule), `CONTRIBUTING.md`'s "How the code is organised" row, `src\Source191.Vantage\README.md`, `openspec\config.yaml`'s context and `LayeringShould`'s summary. Verify by reading each against design.md and checking no em dash was introduced.
- [ ] 5.7 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next preview above `main` (0.20.0-preview.8 if nothing lands first). Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally against `main`.

## 6. Whole-solution check

- [ ] 6.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate tidy-composition-root --strict` all pass. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- Push and open the implementation pull request only when the owner asks.
