# Design

## Context

See proposal.md for why. Main at `356588b`. This change lands after `null-suppressions`, which removes the `!` operators in `src\` (one is in `EditorChoice.FromFile`) and changes `ReleaseVersion.TryParse`; where both touch a line, this change rewrites what that one left. The `!` after `ProcessTree.Under(...)` in `MemoryBreakdownShould` goes here, with the answer it suppresses.

Today's shapes and their readers:
- `ClaudeCli.InterpretStart` matches `backgrounded\s+\S+\s+(?<id>[0-9a-f]{8})\b` and returns `StartResult(Started, id, stdout)`, or `(WorkspaceNotTrusted or Failed, null, message)`. `LaunchService` maps a non-started or id-less result to `LaunchOutcome(kind, null, message)`, and a started one to `LaunchOutcome(Started, JobId.TryParse(...), message, listed session or null)`. `StartSessionHandler` reads `{ Started, JobId: JobId }` (else `Failed(message)`), `HereCommandRunner` reads `Started when JobId is not null` (else prints `Wording.Here` to the error stream and exits 1). `ClaudeContractShould` asserts the started message contains the CLI's middle dot.
- `ScreenLoop.RunAsync` refuses any exit effect that is not `null or Quit`; `ConsoleScreen.leaving` starts null.
- `WorktreesView.RowAction` and `ConfigView.RowAction` take a `decide` answering `(ViewEffect?, Footer)`; every pair with an effect has `Footer.None`, and every pair without one has the footer to show (`Footer.None` for a worktree whose pull request is still loading).
- `DialogEffects.ConfirmUpdate` tests `status is not UpdateStatus.Available`, then shows `UpdateVantage.Refusal(status) ?? new Footer.UpToDate()`.
- Every `Confirm.Query` caller tests `answer != 0`.

## Goals / Non-Goals

**Goals:**
- Every result and answer listed in the proposal says which case it is by its type, so a switch over it is checked by the compiler.
- No visible change: footers, outcome sentences, exit codes and dialog answers stay word for word.

**Non-Goals:**
- State held in rows, views or the Tui, and port answers other than `StartResult` (the sibling changes in the proposal).
- Replacing `FirstOrDefault` locals, `TryParse` outs or guard clauses, which stay.

## Decisions

**Named hierarchies, one per concept, no generic answer type.** Each new type says what its cases mean (`NotListedYet`, `NeverSentAMessage`, `NotMeasurable`). A closed hierarchy is switched with every case named and a discard arm that throws `UnreachableException`, as CLAUDE.md asks of `SessionNameValidation`; an open one (`Outcome`, `Footer`) keeps its fallback. Alternative: one `DialogAnswer<T>` for every dialog (shorter, but a generic Option under another name, which the owner ruled out).

**The job id is parsed where the CLI's text arrives.** `StartResult` becomes `abstract record StartResult(string Message)` with `Started(JobId JobId, string Message)`, `WorkspaceNotTrusted(string Message)` and `Failed(string Message)`. `ClaudeCli.InterpretStart` builds `Started` only when `JobId.TryParse` reads the matched id, otherwise `Failed(stdout)`. The pattern's eight hex characters are exactly what `JobId` accepts, so that fallback cannot be reached; it exists so the adapter never trusts text it did not check. Today the same unreachable case would print "Started name () in folder." and exit 1 from `vantage here`; after it would print "Could not start: <the CLI's output>." and exit 1. No reachable answer changes. The contract facts read the case with `ShouldBeOfType<StartResult.Started>` and keep their message and middle-dot checks.

**`LaunchOutcome` keeps "started" as a case of its own.** `abstract record LaunchOutcome` with `abstract record Started(JobId JobId)`, `sealed record Listed(JobId JobId, BackgroundSession Session) : Started`, `sealed record NotListedYet(JobId JobId) : Started`, `WorkspaceNotTrusted`, and `Failed(string Message)`. `HereCommandRunner` attaches on `Started` whichever way it was listed; `StartSessionHandler` opens the tab on `Listed` and answers `StartSessionOutcome.NotListedYet` on `NotListedYet`, as now; `Wording.Here` words `Started` (with its job id), `WorkspaceNotTrusted` and `Failed` exactly as today. `LaunchService` keeps its `ListedSessionAsync` lookup (a `FirstOrDefault` local) and turns its answer into `Listed` or `NotListedYet` on the next line. A git failure while creating the worktree is `Failed(message)`, as now.

**`ConsoleExit.Effect` is a `ViewEffect`.** `ConsoleScreen` declares `private ViewEffect leaving = new Quit();`, so a window closed without a key (Terminal.Gui's own quit) leaves with `Quit`, which `ScreenLoop` already treats the same as null. `ScreenLoop` then refuses anything that `is not Quit`. Alternative: a `ConsoleExit.Quit(state)` factory (one more name for one constructor call).

**`RowDecision` is one internal two-case record in `Application\Views`.** `Act(ViewEffect Effect)` runs the effect with `Footer.None`; `Explain(Footer Footer)` shows the footer and runs nothing. Both views' `RowAction` take `Func<TRow, RowDecision>` and switch over the two cases. The worktree whose pull request is still loading answers `Explain(Footer.None)`, as its `(null, Footer.None)` does now. It is shared because both views use the same rule; internal because nothing outside Core reads it.

**`UpdateVantage.Decide(UpdateStatus)` answers an `UpdateDecision`: `Ask(ReleaseVersion Newest)` or `Refuse(Footer Footer)`.** `Available` asks; `UpToDate` refuses with `Footer.UpToDate`; `Off` refuses with `Footer.UpdateCheckOff`. `DialogEffects.ConfirmUpdate` switches over the decision, so its separate `is not Available` test and its `?? new Footer.UpToDate()` fallback go. `UpdateStatus` is closed, so `Decide` names its three cases.

**`ResumeNote` becomes `NeverSentAMessage(string Name)` and `Failed(string Name, string Failure)`.** `WordingOfOutcomes.Describe(ResumeSessionsOutcome)` words each case as today ("never sent a message, so there is nothing to resume; d deletes it", and "name: failure").

**Validation answers a value; private helpers stop answering null.**
- `EditorChoice.Problem` becomes `EditorChoice.Validate(string executable, IReadOnlyList<string> arguments)` answering `EditorChoiceValidation.Valid(EditorChoice Editor)` or `Refused(SettingsProblem Problem)`, checked in the same order. `Custom` throws on `Refused`; `FromFile` reads VS Code on `Refused`; `Settings.Validated` refuses with the problem or keeps the editor. The private constructor builds the `Valid` case, so the check runs once.
- `Settings.Validate`'s roots check returns `Refused(...)` from each arm of its switch and `Validated(text, roots)` from the last, in place of a `SettingsProblem?` local.
- `SessionName` has one rule, so its private `ProblemWith` becomes `IsName(string text)`; `ReportTitle` has two, so its helper becomes `HasProblem(string text, out ReportTitleProblem problem)` (an enum out, never null). `Validate` and `TryParse` answer exactly as now; CLAUDE.md's "a `Validate` that returns the reason as a value" stays true.

**Dialogs answer what their caller tests.**
- `Confirm.Ask(...)` answers true when the first button was chosen; all four callers (delete branch, close empty, update, delete session) test exactly that today.
- `NameDialog` and `TypedConfirmationDialog` answer a Tui `TypedAnswer`: `Typed(string Text)` or `Cancelled`. `DialogEffects` reduces `RenameConfirmed(session, text)` or `RenameCancelled(session)`, and `ForceConfirmed(row, expected, typed)` or `ForceCancelled(row)`. The reducers show `Footer.RenameCancelled` and `Footer.ForceCancelled` for the cancelled inputs, as they do for a null today; a blank or wrong typed text keeps its footers.
- `ChooseSolutionDialog` answers a Tui `SolutionChoice`: `Chosen(string File)` (the dialog checks the index against the list it showed) or `NoneChosen`.
- `LaunchWizardDialog` answers the `WizardEffect` it ended on (`Launch` or `Close`) and `SettingsDialog` the `SettingsEffect` (`Save` or `Close`), starting at `Close` as `ProblemReportDialog` starts at `ProblemReportEffect.Close`. `WizardResult` goes. `DialogEffects` dispatches on `Launch` and `Save` and does nothing on `Close`; the dialogs never end on `None`, so the switches' last arm throws `UnreachableException`.

**Lookups answer a case.**
- `DetailRequests.Accept` answers `DetailAnswer.Show(DetailContent Content)` or `DetailAnswer.Dropped`. Its `selected` field starts at, and `Cleared` sets it to, a private `static readonly object NoRow = new()` that no real key equals. `TableBody` reads `is not DetailAnswer.Show shown` on its existing line; its own nullable fields are `null-free-screens`.
- `ProcessTree.Under` answers `ProcessTreeLookup.Found(ProcessTree Tree)` or `NotMeasurable` (the root is not in the table or cannot be measured). `DialogEffects.ShowMemoryOf` switches over it on the UI thread (`Found` shows the breakdown, `NotMeasurable` shows `Footer.NothingToMeasure`), so `Measure` and its `MemoryBreakdown?` go. `SessionRowEnricher` reads `Found found ? found.Tree.Total` with `MemorySize.Zero` for an automated run that has gone (as now), and with `null` for a session's row until `null-free-rows` gives `SessionRow.Memory` a case of its own. `ProcessTree.Usage` (a member that refused) stays for `null-free-rows`.
- `PullRequests.ForBranch` answers `PullRequestLookup.Found(pullRequest)` or `Missing(PullRequestGap.NoPullRequest)`, which is what `PullRequestCatalogue`'s private `ForBranch` builds from its null today; the catalogue's helper becomes a filter and a call. `Missing.Host` stays for its sibling.

**Dropped:** `KeyMapping.From` (`Key?`) is the translation of Terminal.Gui's key and its null never leaves `TableBody.OnKey`'s first lines, so it goes with `null-free-screens`, which owns that file's state. `ConfigView.PathOf` (`string?`) and `ProblemReportView.TooLong` feed view state and go with it too.

## Risks / Trade-offs

- [Many small types] → each is two or three cases in the file of the thing it answers for, and each replaces a comment that explained what null meant.
- [`StartResult` is a port record with three readers outside Core (`ClaudeCli`, the contract facts, `ClaudeCliShould`)] → the contract facts run nightly and on demand with `VANTAGE_CLAUDE_CONTRACT=1`; task 2.1 runs them once before the pull request.
- [The dialogs are Tui code with no unit tests] → their callers' reducers and outcomes are tested in Core; the smoke check opens and cancels the wizard, the settings dialog and the force remove dialog, which exercises the `Close` and `Cancelled` paths.
- [A merge from main moves the cited lines again] → each task names the member, not the line.
