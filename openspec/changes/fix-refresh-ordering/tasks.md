# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, or the Infrastructure tests where the task says so.

## 1. The views' rules

- [ ] 1.1 `ListSelectionShould.KeepUnloadedWhenAFilterIsEditedBeforeTheFirstLoad`: `Alphabetical.EmptyList().StartFilter().EditFilter(Key.Of('a')).Loaded`, the same through `EditFilter(Key.Backspace)` and through `ClearFilter()`, all false, and `Loaded.StartFilter().EditFilter(Key.Of('p')).Loaded` still true. Red, then `Reselect` keeps `Loaded`. Verify the fact and `BeLoadedOnlyAfterTheFirstLoad` pass.
- [ ] 1.2 Extend `SessionsViewShould.ClearAMessageWhenTheSelectionMovesOrTheFilterChanges`, `WorktreesViewShould.ClearTheLastMessageWhenTheSelectionMovesSoTheNewRowsReasonShows` and `ConfigViewShould.ClearAMessageWhenTheFilterChanges` with Escape on a state holding a committed filter and a `Kept` message: the filter is cleared and `Message` is `StatusMessage.None`. Red, then `with { Message = StatusMessage.None }` on the three Escape arms. Verify the three facts pass.
- [ ] 1.3 `SessionsViewShould.DoNothingOnEscapeWithNoFilter`: Escape on `Loaded with { Message = new StatusMessage.Kept() }` leaves the state equal (the message still showing) and raises no effect; red only once 1.2 has made the unguarded arm clear the message. Then the `when state.Filter.Text.Length > 0` guard on `SessionsView.cs:32`. Verify the fact passes and `CommitTheFilterWithEnterAndClearItWithEscape` still passes.

## 2. Load ordering

- [ ] 2.1 `LoadSequenceShould.AcceptLoadsInTheOrderTheyBegan` (`Begin()` answers 1, 2, 3; `Accept` of each in turn is true) and `LoadSequenceShould.DropAResultOlderThanTheLastApplied` (begin two, accept the second, the first is refused, a third begun later is accepted). Red, then `Application\LoadSequence` (sealed, its own file). Verify both pass.
- [ ] 2.2 `Tui\TableBody`: a `LoadSequence` field; `TriggerLoad` takes `int ticket = sequence.Begin()` before `Task.Run` and its `app.Invoke` continuation returns before `Apply(input)` when `sequence.Accept(ticket)` is false, so no `AskForDetail` either. Verify the build and `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>` pass, and by hand that `x` on a session never shows the row again after the stop.

## 3. The process runner

- [ ] 3.1 `ProcessRunnerIntegrationShould.EndAChildThatOutlivesTheLimitAndReportIt`: a runner with a one-second limit runs `cmd.exe /c ping -n 30 127.0.0.1` (the cmd hop exercises `entireProcessTree`), throws `ProcessTimedOutException` within about two seconds whose message names `cmd.exe` and 1 second, and no `ping` started after the test began survives (`Process.GetProcessesByName("ping")` filtered by `StartTime`). Red, then `Ports\ProcessTimedOutException` (sealed, own file, `FileName` and `Limit`, message as design.md) and the linked token, kill and throw in `ProcessRunner.RunAsync`, with `ProcessRunner.DefaultLimit` 60 seconds. Verify in `test\Source191.Vantage.Infrastructure.Tests`.
- [ ] 3.2 `ProcessRunnerIntegrationShould.EndAChildWhenItsCallerCancels`: the same child with the default limit and a token cancelled after 200 ms throws `OperationCanceledException` within about a second and leaves no `ping` behind. Red, then the kill on the caller's cancellation. Verify in the Infrastructure tests; `CaptureStandardOutputAndTheExitCode` and the other runner facts still pass.
- [ ] 3.3 `RecordedProcessRunnerShould.RecordARunThatTimedOutAndRethrow`: the inner runner throws `ProcessTimedOutException`, the log holds one `ProcessTimedOut(ChildCommand.For(Agents), TimeSpan.FromSeconds(60))`, no `ProcessFailedToStart`, and the exception reaches the caller. Red, then the event (`Application\Diagnostics\ProcessTimedOut.cs`) and the catch in `Started`. Verify the fact, `DiagnosticEventsShould` and `DiagnosticsPrivacyShould` pass.
- [ ] 3.4 `DiagnosticLineShould` fact: `ProcessTimedOut` renders as `timed out claude agents after 60000 ms`. Red, then the `DiagnosticLine` arm. Verify the fact passes.
- [ ] 3.5 `ClaudeCliShould.AnswerNotReadWhenTheRunTimesOut`: a `FakeProcessRunner` failing with `ProcessTimedOutException` gives `ClaudeVersionReport.NotRead`. Red, then the type added to `VersionAsync`'s `when` filter. Verify the fact passes.

## 4. The console screen

- [ ] 4.1 `Tui\ConsoleScreen`: a `RunningLoads listings` constructor parameter, `ListSessions()` beside `ScanWorktrees()` reserving it, `StartRefreshing` calling `ListSessions` from the timer and at start-up; `Program.cs` builds `RunningLoads listings` and hands `listings.Track<ViewInput>(LoadAsync)` and `listings` to the screen and `ProcessRunner.DefaultLimit` to the runner. Verify the build and the smoke check, and with `VANTAGE_LOG` set that the exit counts show one claude `agents` run per five seconds of a quiet console, never more, and `RunningLoadsShould` still passes (the sessions reservation reuses it unchanged).
- [ ] 4.2 `Tui\TableBody`: `int inFlight` for `bool running`, `Running => inFlight > 0`, and the continuation decrementing first, then `StatusMessage settled = settle();`, then `State = definition.WithMessage(State, settled)`. Verify the build and the smoke check, and by hand: `m` on a session left open over a listing closes without the rows jumping back; `d` on two worktrees in a row keeps the spinner until the second reports.
- [ ] 4.3 README: the troubleshooting list gains a line saying a listing or scan that fails with "did not finish within 60 seconds" means Claude Code or git did not answer and Vantage ended it, and the next refresh retries. Verify by reading against the spec deltas and that no em dash was introduced.

## 5. Version and whole-solution check

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above the one on `main` when implementing (patch, minor or major as CONTRIBUTING's Versions says; patch here: fixes and no new key). Verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes.
- [ ] 5.2 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate fix-refresh-ordering --strict` all pass; then `tools\install-local.ps1` and by hand: `/` and a letter before the first listing keeps "Listing sessions"; Esc with a filter and a message set clears both on each tab; the hand checks of 2.2 and 4.2.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\session-listing`, `worktree-management`, `console-layout`, `external-tools` and `diagnostics` describe the new behaviour.
