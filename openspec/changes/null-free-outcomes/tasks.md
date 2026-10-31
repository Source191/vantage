# Tasks

Each task is one red-green-refactor cycle with a commit per step, the test named before the code. Unless a task says otherwise, verify it with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, and every expected footer, sentence and exit code in the facts it touches stays as it was.

## 1. Domain: validation and lookups

- [x] 1.1 `EditorChoiceShould.RefuseACustomCommandWithTheFirstProblem` (blank, bare name, batch file, no placeholder, in that order) and `AcceptACustomCommandAsValid`: red, then `EditorChoice.Validate` answering `EditorChoiceValidation.Valid` or `Refused` green; `Custom`, `FromFile` and `Settings.Validated` move onto it and `Problem` goes. Verify `EditorChoiceShould` and `SettingsShould` pass with their expected problems unchanged and `EditorChoice.cs` declares no `SettingsProblem?`.
- [x] 1.2 Refactor `Settings.Validate`'s roots check to return from each switch arm (no new fact). Verify `SettingsShould`'s `Refuse...` facts pass unchanged and `Settings.cs` has no `SettingsProblem?`.
- [x] 1.3 Refactor `SessionName.ProblemWith` to `IsName` and `ReportTitle.ProblemWith` to `HasProblem(text, out ReportTitleProblem)` (no new fact). Verify `SessionNameShould` and `ReportTitleShould` pass unchanged and neither file declares a nullable problem.
- [x] 1.4 `ProcessTreeShould`'s facts expect `ProcessTreeLookup.Found` with the same tree, and `NotMeasurable` for a root not in the table and for a root that cannot be measured (the two `ShouldBeNull` go): red, then `ProcessTree.Under` answering a `ProcessTreeLookup` green. Verify `ProcessTreeShould` passes.
- [x] 1.5 `PullRequestsShould` expects `PullRequestLookup.Found(older)`, `Found(merged)` and, for an empty list, `Missing(PullRequestGap.NoPullRequest)`: red, then `PullRequests.ForBranch` answering a `PullRequestLookup` green. Verify `PullRequestsShould` and `PullRequestCatalogueShould` pass, the catalogue's private `ForBranch` no longer tests for null.

## 2. Port and adapter: starting a session

- [ ] 2.1 `ClaudeCliShould`'s start facts expect `StartResult.Started` with `JobId.From(...)` and the CLI's message, `WorkspaceNotTrusted` and `Failed`: red, then the `StartResult` hierarchy in `Ports\IClaudeCli.cs` (the `StartOutcome` enum goes) and `ClaudeCli.InterpretStart` parsing the id green. `ClaudeContractShould`'s two start facts read the case with `ShouldBeOfType<StartResult.Started>`. Verify `dotnet test test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build` passes, then the contract facts from a trusted repository: `$env:VANTAGE_CLAUDE_CONTRACT = 1; dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug -- --filter-trait "Category=ClaudeContract"`.

## 3. Application: outcomes and decisions

- [x] 3.1 `LaunchServiceShould` expects `LaunchOutcome.Listed` (with the listed session), `NotListedYet`, `WorkspaceNotTrusted` and `Failed` (a git failure, a CLI failure): red, then the `LaunchOutcome` hierarchy with `LaunchService` building it green (`LaunchOutcomeKind` goes). In the same cycle `StartSessionHandler` and `HereCommandRunner` switch over the cases (every case named, `UnreachableException` discard). Verify `LaunchServiceShould`, `StartSessionHandlerShould` and `HereCommandRunnerShould` pass with their outcomes, printed lines and exit codes unchanged.
- [x] 3.2 `WordingOfOutcomesShould`'s `Here` facts pass `LaunchOutcome.Listed`, `WorkspaceNotTrusted` and `Failed` and expect the same three sentences: red on the old constructor, then `Wording.Here` over the cases green. Verify the facts pass word for word.
- [x] 3.3 `ResumeSessionsHandlerShould` expects `ResumeNote.NeverSentAMessage("repos-f6")`, and `WordingOfOutcomesShould`'s resume fact passes `NeverSentAMessage` and `Failed("fix-123", "boom")` with the same sentence: red, then the `ResumeNote` hierarchy, the handler and `Describe(ResumeSessionsOutcome)` green. Verify both facts pass word for word.
- [x] 3.4 `UpdateVantageShould.AskToUpdateWhenAnUpdateIsKnown`, `SayUpToDateOtherwise` and `SayTheCheckIsOff` expect `UpdateDecision.Ask(newest)`, `Refuse(Footer.UpToDate)` and `Refuse(Footer.UpdateCheckOff)`: red, then `UpdateVantage.Decide` green and `Refusal` goes. Verify the three facts pass.
- [x] 3.5 Refactor `WorktreesView` and `ConfigView`'s row keys onto an internal `RowDecision` (`Act`, `Explain`) in `Application\Views\RowDecision.cs` (no new fact). Verify `WorktreesViewShould` and `ConfigViewShould` pass unchanged and neither view declares `ViewEffect?`.
- [x] 3.6 `ScreenLoopShould.StartOnTheSessionsTabAndQuitWhenTheConsoleReturnsNoEffect` becomes `StartOnTheSessionsTabAndQuitWhenTheConsoleLeavesWithQuit`, and `RunTheScreenOnceAndRefuseAnyOtherWayOut` keeps its attach exit: red on the non-nullable `ConsoleExit`, then `ConsoleExit(ConsoleState, ViewEffect)` and `ScreenLoop` refusing anything that `is not Quit` green. Verify `ScreenLoopShould` passes.
- [x] 3.7 `SessionsViewShould.AskForANewNameOnRAndRenameOnceOneIsGiven` reduces `RenameCancelled(WaitingJob)` where it reduced `RenameConfirmed(WaitingJob, null)`, and `WorktreesViewShould.ForceOnlyWhenTheTypedTextMatchesAndSayWhatWasTypedOtherwise` reduces `ForceCancelled(Dirty)` where it passed `Typed: null`, each expecting the same cancelled footer: red, then the two cancelled inputs, non-nullable `Name` and `Typed`, and the reducers' cases green. Verify both facts pass with `Footer.RenameCancelled` and `Footer.ForceCancelled` as before.
- [ ] 3.8 `DetailRequestsShould` expects `DetailAnswer.Show` (with the same title) and `DetailAnswer.Dropped` where it expected not-null and null: red, then `Accept` answering a `DetailAnswer` and the `NoRow` sentinel in place of the null selection green. Verify `DetailRequestsShould` passes and `DetailRequests.cs` declares no nullable.
- [ ] 3.9 `MemoryBreakdownShould` reads the tree through `ShouldBeOfType<ProcessTreeLookup.Found>().Tree` (its `!` goes) and `SessionRowEnricherShould`'s memory facts pass unchanged: refactor `SessionRowEnricher` onto `ProcessTreeLookup`. Verify both pass and an automated run that has gone still counts `MemorySize.Zero`.

## 4. Tui: dialogs and the console

- [ ] 4.1 `Confirm.Query` becomes `Confirm.Ask` answering `bool`; `NameDialog` and `TypedConfirmationDialog` answer `TypedAnswer`; `ChooseSolutionDialog` answers `SolutionChoice`; `LaunchWizardDialog` answers `WizardEffect` and `SettingsDialog` `SettingsEffect` (both starting at `Close`), and `WizardResult.cs` goes. `DialogEffects` switches over each answer, reduces `RenameCancelled` and `ForceCancelled`, switches over `UpdateVantage.Decide` and over `ProcessTreeLookup` in `ShowMemoryOf`; `ConsoleScreen.leaving` starts at `new Quit()`; `TableBody` reads `DetailAnswer.Show` (refactor step: `Tui` has no test project). Verify the build, and that `Get-ChildItem src\Source191.Vantage.Tui -Filter *.cs | Select-String 'int\? answer|string\? typed|WizardResult|Settings\? saved|ViewEffect\? leaving|MemoryBreakdown\?'` finds nothing.
- [ ] 4.2 Smoke check of the packed tool: pack locally (`tools\install-local.ps1` or `dotnet pack`) and run `dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>`, which opens and cancels the wizard, the settings dialog and force remove. Verify exit 0 and record the run here.

## 5. Version and the whole build

- [ ] 5.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one step above main's (projected `0.19.0-preview.5`). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `origin/main`.
- [ ] 5.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate null-free-outcomes` all pass.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
