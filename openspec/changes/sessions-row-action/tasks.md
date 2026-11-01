# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step. Most steps are refactors kept green by the facts already in `test\Source191.Vantage.Core.Tests\Application\Views`; where a step is a pure refactor with no red, the commit says so. Verify with `dotnet build -c Debug` (warnings are errors) and `dotnet test --project test\Source191.Vantage.Core.Tests -c Debug --no-build` unless a task says otherwise. Groups 1 and 2 are the Application record and helper, group 3 the Sessions reducer, group 4 the Tui and the tidy-up, then the version and the whole-solution check.

## 1. One update record

- [ ] 1.1 `ViewUpdate.cs` becomes `ViewUpdate<TState>(TState State, IReadOnlyList<ViewEffect> Effects)` with static `Unchanged` and `Effect`; `SessionsView` returns `ViewUpdate<SessionsViewState>` through its `using static`, its two private helpers deleted; `SessionsViewShould`'s `ViewUpdate` locals (32) become `ViewUpdate<SessionsViewState>`. Verify the fast suite passes and `OneTypePerFileShould`, `SealedTypesShould` and `NullFreeModelsShould` stay green.
- [ ] 1.2 `WorktreesView` and `ConfigView` return the same record through their own `using static`; `WorktreesViewUpdate.cs` and `ConfigViewUpdate.cs` and the four helpers are deleted; `WorktreesViewShould` (11) and `ConfigViewShould` (4) declare the generic. Verify the fast suite passes and `grep` finds no `WorktreesViewUpdate` or `ConfigViewUpdate` under `src` or `test`.

## 2. The shared keys

- [ ] 2.1 `SharedKeysShould.AnswerTheEffectOfASharedKey` (a theory: `q` `Quit`, `?` `ShowHelp`, `U` `UpdateVantage`, `!` `ShowProblemReport`) and `AnswerNoEffectForAKeyThatIsNotShared` (`1`, `x`, Enter), shaped as `TabKeysShould`. Red (the type does not exist), then green with `SharedKeys.TryFor` in `src\Source191.Vantage.Core\Application\Views\SharedKeys.cs`, its `out` attribute the one `TabKeys.TryFor` has on `main`. Verify the facts pass.
- [ ] 2.2 Each view's four arms become one `_ when SharedKeys.TryFor(key, out ViewEffect? shared) => Effect(state, shared)` arm before the `TabKeys` arm. Verify `SessionsViewShould.EmitTheEffectForTheKey` (its `q`, `?`, `U`, `!` rows), `WorktreesViewShould.AskToUpdateOnCapitalU`, `GoBackToTheSessionsViewOnWOrEscapeAndQuitHelpAsUsual`, `ReportAProblemOnBang` and `ConfigViewShould.EmitTheEffectForTheKey` pass, and `grep` finds `new Quit()` in the three views only at `UpdateTabOpened`.

## 3. RowAction on Sessions

- [ ] 3.1 Pin before moving, green at once: `SessionsViewShould.RefuseToStopOrRenameAnInteractiveRow` (`x` and `r` on the interactive row answer `RunsInOwnTerminal("repos-93", HasTranscript: true)` and no effect) and `SayNoSessionIsSelectedOnAnEmptyTable` (a theory over `x`, `d`, `r`, `b`, plus Enter, on `SessionsViewState.Empty`). Verify they pass against today's code and commit them alone.
- [ ] 3.2 `RowAction(SessionsViewState, Func<SessionRow, RowDecision>)` in `SessionsView`, the same lines as `WorktreesView.RowAction`; Enter becomes `RowAction(state, Opening)` and `Open` is deleted. Verify `OpenTheSelectedBackgroundSessionInItsOwnTabOnEnter`, `RefuseActionsOnAnInteractiveRow`, `RefuseToOpenAStoppedSessionThatWasNeverSentAMessageBecauseNothingCanResume`, `SwitchToTheLiveTabThatShowsTheSessionOnEnter` and `ConsoleViewShould` pass.
- [ ] 3.3 `OnJob(SessionRow, Func<BackgroundSession, ViewEffect>)` as a `RowDecision`; `x` becomes `RowAction(state, row => OnJob(row, job => new StopSession(job)))`; `d` becomes `RowAction(state, Delete)` with `Delete(row)` deciding `CloseEmptyTerminals` for an interactive row without a transcript and otherwise `OnJob(row, job => new DeleteSession(job))`, so the two `d` arms are one and `C` follows it; `SessionAction` is deleted. Verify `StopCarriesTheSelectedRow`, `OfferToCloseATerminalSessionThatHasNoMessagesAndCloseItOnX`, `KeepRefusingToDeleteATerminalSessionThatHasAConversation`, `CloseEveryTerminalSessionWithNoMessagesOnCAndSayWhenThereAreNone`, `EmitTheEffectForTheKey` (`x`, `d`) and 3.1's facts pass.
- [ ] 3.4 `c`, `f` and `s` become `RowAction` arms building `RowDecision.Act` from `row.Session.WorkingDirectory` and `row.Repository`; `FolderAction` is deleted. Verify `CopyTheSelectedSessionsFolder`, `OpenTheSelectedSessionsFolderInTheEditorWhateverKindOfSessionItIs`, `FindTheSelectedSessionsSolutionByItsFolderAndRepository`, `SayNoSessionIsSelectedWhenAFolderKeyIsPressedOnAnEmptyTable`, `ClearTheLastMessageWhenAFolderKeyActs` and `MarkTheSolutionSearchAsAskingFirstAndTheOtherFolderEffectsAsBackground` pass.
- [ ] 3.5 `p` and `m` become `RowAction(state, PullRequestOf)` and `RowAction(state, Memory)`, each a `Func<SessionRow, RowDecision>` in today's arm order; `PullRequestOf` names `Found` and `Missing` and its discard throws `UnreachableException` in place of `_ => Unchanged(state)`. Verify `OpenTheSelectedRowsPullRequest`, `ExplainAMissingPullRequest`, `NeedASelectionToOpenAPullRequest`, `ShowTheMemoryOfARunningSessionOnM`, `ShowTheMemoryOfATerminalSessionOnM`, `SayThereIsNothingToMeasureForAStoppedSession` and `SayNoSessionIsSelectedOnM` pass.
- [ ] 3.6 `b` and `r` become `RowAction(state, Adopt)` and `RowAction(state, Rename)`; `Adopt` names `BackgroundSession` and `InteractiveSession` with a throwing discard; `Rename`'s first arm answers `RunsInOwnTerminal` for a non-background row, so the lambda at `SessionsView.cs:170` goes. Verify `AdoptAnIdleInteractiveSessionOnB`, `RefuseToAdoptAnInteractiveSessionWhileItIsWorking`, `ExplainThatABackgroundSessionNeedsNoAdoption`, `AskForANewNameOnRAndRenameOnceOneIsGiven`, `RefuseToRenameASessionWhileItIsWorkingBecauseRenamingRestartsIt`, `EmitTheEffectForTheKey` (`r`) and 3.1's facts pass, and `new StatusMessage.NoSessionSelected()` appears once in `SessionsView.cs`.
- [ ] 3.7 `SessionsView` gets a class summary like `WorktreesView`'s, naming `RowAction` and `Opening`; the missing blank line before the summary at `SessionsView.cs:130-131` is gone with `FolderAction`, and the double blank line at `SessionsViewState.cs:34-35` goes. Verify `dotnet format --verify-no-changes` passes.

## 4. The Tui, the guard, Shown and the parentheses

- [ ] 4.1 `IScreenDefinition<TState, TRow>.Reduce` returns `ViewUpdate<TState>`; the three definitions become one-line delegations (IDE0022) and `TableBody.Apply` reads `update.State` and `update.Effects`. Verify `dotnet build -c Debug` and `dotnet test -c Debug --no-build` at the solution pass and the tuple `(TState State, IReadOnlyList<ViewEffect> Effects)` appears nowhere under `src`.
- [ ] 4.2 `ConfigView.Reduce` loses `ArgumentNullException.ThrowIfNull(state)`, so no reducer guards its state. If `convention-nets` has landed with the rule that every public method guards, add the guard to `SessionsView.Reduce` and `WorktreesView.Reduce` instead and record that here. Verify `ConfigViewShould` passes and the build is clean.
- [ ] 4.3 `IListViewState<TRow>` declares `StatusMessage Message { get; }` and loses `Shown`; the three states lose `Shown => Message`; `TableBody.Status => State.Message`; `SessionsViewShould.ShowNothingUntilThereIsAMessage`, `WorktreesViewShould.ShowOnlyMessagesTheHealthBeingThePanes` and `ConfigViewShould.ShowNoMessageForTheSelectedRowAlone` assert `.Message`, dropping `ConfigViewShould.cs:196`. Verify those facts pass, the solution builds, and `grep -n "\.Shown\b"` under `src` matches only `TableTitle.Shown` in `WordingOfScreens.cs` and `ITabBody.Shown(bool)`'s callers in `ConsoleScreen.cs`.
- [ ] 4.4 `WorktreesView.InRealWorktree` reads `(row.Health.Kind is WorktreeHealthKind.Ok or WorktreeHealthKind.Locked or WorktreeHealthKind.Elsewhere) && row.FolderExists`. Verify `StartASessionOrAShellInARealWorktreeOnly`, `TreatAnElsewhereWorktreeAsRealForSessionsAndShells` and `RefuseAFolderKeyOnALeftoverWhoseFolderIsGone` pass.

## 5. Version

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above `main` when implemented (0.20.0-preview.9 if nothing lands first); patch-level, a refactor. Verify `.github\scripts\Assert-VersionBumped.ps1` passes locally against `main`.

## 6. Whole-solution check

- [ ] 6.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate sessions-row-action --strict` all pass. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- Push and open the implementation pull request only when the owner asks.
