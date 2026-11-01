# Design

## Context

`ClaudeCli` (`src\Source191.Vantage.Infrastructure\Claude\ClaudeCli.cs`) runs `agents --json --all` at line 16 and hands its standard output straight to `ClaudeAgentsListing.Parse`; `StopAsync` and `RemoveAsync` (lines 52 and 54) return the process runner's task and never look at the result. `VersionAsync`, `InterpretStart` (lines 80-96) and `ResumeAsync` (lines 62-76) do read the exit code and claude's words, and `InterpretStart` already chooses standard error, else standard output, trimmed. `GitCli` has `RunOrThrowAsync` (`src\Source191.Vantage.Infrastructure\Git\GitCli.cs:171-180`) throwing `GitException`, and `IGit`'s summary states the convention. `ResumeResult` (`src\Source191.Vantage.Core\Ports\ResumeResult.cs:7`) is `(bool Continued, string Message)`, read as a flag at `AdoptSessionHandler.cs:39`, `RenameSessionHandler.cs:29` and `:45`, and `ResumeSessionsHandler.cs:34`. `TerminalEffects.RunAsync` (`src\Source191.Vantage.Core\Application\Actions\TerminalEffects.cs:12-24`) is a `switch` over `ViewEffect` with a silent default, and `HereCommandRunner.AttachAsync` (`src\Source191.Vantage.Core\Application\Launch\HereCommandRunner.cs:39-45`) calls `RecoverAfterChild` after it; neither clean-up is in a `finally`. A failed listing reaches the banner through `TableBody.TriggerLoad` (`src\Source191.Vantage.Tui\TableBody.cs:155`), which turns any exception into `LoadFailed(exception.Message)`, and reaches a start through `LaunchService.ListedSessionAsync` (`LaunchService.cs:59-64`), which does not catch. The handler tests are sociable over the real `ClaudeCli` and `FakeProcessRunner` (`SessionTestData.Processes()`); there is no `FakeClaudeCli`.

## Goals / Non-Goals

**Goals:**
- `IClaudeCli` states for every method what a failure looks like, as `IGit` does, and the adapter keeps to it.
- No key ever runs `--bg --resume` on a job whose `rm` failed.
- Every failure a key reports carries claude's words, none a parser's.

**Non-Goals:**
- Changing how `VersionAsync`, `StartBackgroundAsync` or `ResumeBackgroundAsync` report failure (a report, never a throw).
- Catching the exception in `vantage here` or the wizard's start: a failed listing after a start keeps today's path (the dispatcher's `StatusMessage.Failed`, or `vantage here`'s crash), with better words.

## Decisions

### A `ClaudeCliException` in Ports, thrown by one `RunOrThrowAsync`
`Ports\ClaudeCliException(string message) : Exception`, sealed, in its own file beside `GitException`. `ClaudeCli` gains `RunOrThrowAsync(IReadOnlyList<string> arguments, WorkingFolder folder, CancellationToken)` that runs the request and throws when the exit code is not 0; `ListSessionsAsync`, `StopAsync` and `RemoveAsync` go through it. The summary on `IClaudeCli` reads as `IGit`'s: `ListSessionsAsync`, `StopAsync` and `RemoveAsync` throw `ClaudeCliException` with claude's words when claude exits with an error; `StartBackgroundAsync` and `ResumeBackgroundAsync` answer a result in every case; `VersionAsync` answers `NotRead`; `AttachAsync` answers the exit code; and each method's own summary says which. Alternative: a `StopResult`/`RemoveResult` per method, which would give three handlers a case for the one failure the exception already carries and break the parallel with `IGit`.

### The message is claude's sentence alone
`GitException`'s message is `git <args> failed: <stderr>`, because the worktrees banner and the remove outcomes do not name the command. Here the banner's prefix is already "listing failed" and the outcomes read "Could not stop fix-123:", so a `claude stop 81cdb0f9 failed:` prefix would read "failed" twice. The message is standard error trimmed, else standard output trimmed (the choice `InterpretStart` makes), else `claude <subcommand> exited with code <n>` when both are blank, so a message is never empty. `ChildCommandWords` is unaffected: nothing new is logged.

### Unreadable JSON on exit 0 stays a `JsonException`
`RunOrThrowAsync` throws on the exit code only. A listing that exits 0 with output that is not JSON still throws `JsonException` from `ClaudeAgentsListing.Parse`, and the port's summary says so ("or a JSON exception when its output is not the listing"). Alternative: wrap it as "something unreadable" for full parity with `IGit`; declined because the exit-0-not-JSON case has never been seen and the wrapping would have to invent words for it.

### Each handler catches the exception into a failed case of its own outcome
- `StopSessionOutcome` becomes a closed hierarchy: `Stopped(string Name, bool TabClosed)` and `NotStopped(string Name, string Failure)`; the tab is not closed on a failure, since the session still runs. Every construction of `StopSessionOutcome` in the tests (`StopSessionHandlerShould`, `WordingShould:328` and `:662`, `WordingOfOutcomesShould:56-57` and `:490`, `StatusTonesShould:45` and `:278`, `EffectDispatcherShould:35`) becomes `StopSessionOutcome.Stopped`.
- `RenameSessionOutcome.NotRemoved(string OldName, string Failure)`: `rm` failed, so nothing was resumed and the tab was not closed.
- `ResumeNote.NotRemoved(string Name, string Failure)`: the session keeps its job and gets no tab; the loop continues with the next session. `Describe(ResumeSessionsOutcome)` at `WordingOfOutcomes.cs:235-240` throws `UnreachableException` on its discard arm, so the new note needs an arm there.
- `DeleteSessionOutcome.NotDeleted(string Name, string Failure)`: `rm` failed, so the tab is kept and the worktree untouched; `DialogEffects.ConfirmDelete` (`DialogEffects.cs:178-183`) reports every outcome but `BranchNotMerged` as it is, so it needs no change.
Alternative: let the exception reach `StatusMessage.Failed` through the dispatcher, which would show the words but still close the tab in stop and still resume in rename and resume-all; the point is the ordering, so the handler must decide.

### Words and tones
`WordingOfOutcomes`: `NotStopped` reads "Could not stop fix-123: <failure>"; `RenameSessionOutcome.NotRemoved` reads "Could not rename fix-123: <failure>; it is still listed under its old name." (no tab clause: none was closed, so none needs mentioning); `ResumeNote.NotRemoved` reads "fix-123: could not remove its job (<failure>), so it was not resumed"; `DeleteSessionOutcome.NotDeleted` reads "Could not delete fix-123: <failure>". The three outcomes are `StatusTone.Failure`; `ResumeSessionsOutcome`'s tone stays computed from its counts and notes. Showing claude's sentence is allowed by the wording rule: an exception's message only to report what failed, in a handler's failed outcome or a load failure.

### `ResumeResult` as a two-case hierarchy
`abstract record ResumeResult(string Message)` with `Continued(string Message)` and `Failed(string Message)`, as `StartResult` is shaped. `ResumeAsync` answers `Failed` for a non-zero exit and for a `note:` line (the "copy was started" case keeps today's meaning), else `Continued`. The three handlers match by case with a discard arm that throws `UnreachableException`, as the two-case record rule requires. Alternative: a third case for the note; declined, since no handler treats the note differently from a failure.

### A typed `RunAsync(AttachToSession)` with the hint removal in `finally`
`TerminalEffects.RunAsync(AttachToSession attach, CancellationToken)` shows the hint, then `try { await claude.AttachAsync(...) } finally { console.RemoveReturnHint(); }`; `Handles` keeps answering `effectType == typeof(AttachToSession)`, so `EffectRoutingShould` passes unchanged and the `switch` and its silent default go. `HereCommandRunner.AttachAsync` wraps the call in `try { ... } finally { console.RecoverAfterChild(); }` and still returns 0 after a normal detach; an exception still propagates as today. The tests make the attach throw through `FakeProcessRunner.OnInteractive` (`FakeProcessRunner.cs:33`), which runs as the interactive request starts, so no fake changes. Alternative: catch and exit 1 with a sentence; out of scope by the proposal, since what `vantage here` prints for a thrown attach is not this change's finding.

## Risks / Trade-offs

- [A failed `claude agents` after a `--bg` start now throws `ClaudeCliException` where it threw `JsonException`] → the same path as today (`LaunchService.ListedSessionAsync` does not catch; the dispatcher shows `StatusMessage.Failed`, `vantage here` crashes), with claude's words instead of a parser's; the resume-all `R` and the wizard are not otherwise affected.
- [A `claude stop` that exits non-zero though the session did stop, in a future CLI] → the tab stays open and the row shows stopped on the next refresh; the user presses `x` again or closes the tab; nothing is lost.
- [Claude prints its reason to standard output, not error, for some failures] → the message takes standard output when standard error is blank, as `InterpretStart` does.
- [The `StopSessionOutcome` reshaping touches eight test files] → each is a one-line rename to `StopSessionOutcome.Stopped`; the build finds every one.
