# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.

## 1. The handler opens a tab for each session it resumes

- [x] 1.1 `ResumeSessionsHandlerShould.OpenATabForEachSessionItResumes`: two sessions with transcripts both resume, and the fake terminal records two attach tabs in the order given, each registered for its session. Red, then `ResumeSessionsHandler` takes `OpenTabHandler` and calls `OpenAsync` after each continued resume; `Composition.Handlers` passes the composition's `tabs`. Verify the fact and the existing handler facts pass.
- [x] 1.2 `ResumeSessionsHandlerShould.OpenNoTabForASessionThatDidNotResume`: one session with no transcript and one the CLI refuses get no tab, and the outcome's notes name both as today. Verify the fact passes.
- [x] 1.3 `WordingOfOutcomesShould`: a `ResumeSessionsOutcome` reads "Resumed 3 of 3, each in its own tab." and, with notes, keeps the notes after it. Red, then `WordingOfOutcomes.Describe` green. Verify the fact passes and `StatusTonesShould` is unchanged.

## 2. R chooses the sessions without a dialog

- [x] 2.1 `SessionsViewShould.ResumeEveryShownStoppedOrFailedSessionInTableOrder`: with a working, a stopped, a failed and an interactive row loaded, `R` raises `ResumeSessions` with the stopped and failed background sessions in the table's order. Red, then the `R` case builds `ResumeSessions` from `VisibleRows`. Replace `HandTheResumeChecklistTheRowsInTheOrderTheTableShowsThem` and the `'R'` line of `EmitTheEffectForTheKey`. Verify the facts pass.
- [x] 2.2 `SessionsViewShould.ResumeOnlyTheSessionsTheFilterShows`: with two stopped rows and a filter matching one, `R` resumes only that one. Verify the fact passes.
- [x] 2.3 `SessionsViewShould.SayThereIsNothingToResume`: with no stopped or failed row shown, `R` raises no effect and sets `StatusMessage.NothingToResume` (renamed from `NothingChosenToResume`, words and Hint tone unchanged in `WordingShould` and `StatusTonesShould`). Verify the facts pass.
- [x] 2.4 Remove `ResumeStoppedSessions`, `ResumePlan`, `ResumeSessionsHandler.Plan` and its fact `SeparateFailedSessionsFromStoppedOnesWhenPlanningAResume`, the `ResumeStoppedSessions` line of the dialog-marker fact, `Tui\ResumeChecklistDialog`, its `DialogEffects` entry and `ChooseSessionsToResume`, and `DialogName.Resume` with its word in `WordingOfScreens` and its line in `WordingOfScreensShould`. Verify the build, `EffectRoutingShould`, `DialogEffectsShould` and `EffectDispatcherShould` pass, and `ResumeChecklist` no longer appears under `src\` or `test\`.

## 3. Words and documentation

- [x] 3.1 The `R` line of `WordingOfScreens.SessionsKeys`: help word "resume every stopped or failed session shown, each in its own tab" and tip "Resume each stopped or failed session shown, in its own tab; / filters which." (shortened from the planned tip, which broke the 80-character rule `GiveEveryKeyATip` keeps) with any fact in `WordingShould` that pins it updated. Verify the facts pass.
- [x] 3.2 README: the key table's `R` row and the "After a reboot" paragraph say `R` resumes every shown stopped or failed session in its own tab, `/` narrows it, Enter resumes one, `d` removes one for good. Verify by reading against the spec delta and checking no em dash was introduced.
- [x] 3.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to `0.20.0-preview.3`, the next preview above `v-resume-continues` (0.20.0-preview.2) in the stack. Verify `Assert-VersionBumped.ps1` passes against `origin/main`.

## 4. Whole-solution check

- [ ] 4.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate resume-without-dialogue --strict` all pass; `tools\install-local.ps1`, then `R` in a real console with stopped sessions resumes them and opens their tabs (manual check from `CONTRIBUTING.md`, as this touches tabs and attach).

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\session-attach` describes `R` as it now behaves.
