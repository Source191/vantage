# Tasks

## 1. Enter opens the tab (Sessions reducer)

- [ ] 1.1 Red: `SessionsViewShould.Enter_opens_the_selected_session_in_its_own_tab` asserts Enter on a background session returns `OpenTabForSession` for it, and `Enter_switches_to_a_live_tab` that a row with a live tab returns `OpenTabForSession` with `HasLiveTab` true rather than a footer. Green: Enter's arm raises `OpenTabForSession` with `Attachment.InOwnTab`. Verify `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`.
- [ ] 1.2 Red: `SessionsViewShould.O_does_nothing` asserts `o` leaves the state unchanged with no effect; the existing Enter refusals (no selection, a terminal session, a stub session never sent a message) are restated for Enter with the footers `o` gave. Green: remove the `o` arm; delete `Attachment.Here`, `Footer.OpenInAnotherTab` and its words in `Wording.Describe`, and the facts that asserted them. Verify the Core tests pass and no `AttachToSession` is raised by `SessionsView`.

## 2. The Sessions tab's words

- [ ] 2.1 Red: `WordingOfKeysAndColumnsShould` asserts the Sessions help lists Enter as "open in its own tab, or switch to it" and no `o` line, and `WordingShould` that `SessionsBar` for a running and a stopped background session starts Enter Open, x Stop / Enter Open, d Delete with no `o`, and still fits 120 columns. Green: Enter's `KeyHelp` (hint Open, the tooltip `o` had), the `o` line removed, both key bar rows. Verify the Core tests pass.
- [ ] 2.2 Red: `WordingShould` asserts `Wording.ReturnHint` is "Ctrl+Z detaches; the session keeps running." and `TerminalEffectsShould` that the attach prints it. Green: the words. Verify the Core and Infrastructure tests pass (`TerminalConsoleShould` passes the hint through unchanged).
- [ ] 2.3 README: the Sessions keys table (Enter Open, no `o`, no attach in this tab), the "Attach in this tab" section removed and the open-in-tab section naming Enter, and the walkthroughs that say Enter then Ctrl+Z. Verify the README's keys match `Wording.SessionsKeys`.

## 3. The wizard starts and opens (reducer)

- [ ] 3.1 Red: `WizardViewShould` asserts `WizardInput.Start` (no argument) returns `WizardEffect.Launch` with the plan, and Enter in a fixed folder's name the same effect. Green: drop `OpenInTab` from `WizardInput.Start` and `WizardEffect.Launch`. Verify the Core tests pass.
- [ ] 3.2 Red: `WordingShould` asserts the wizard hints read "… Enter or Tab next field   F5 start and open in tab   Esc cancel" and, on a fixed folder, "Enter or F5 starts and opens in tab   Esc cancel". Green: `Wording.WizardHint`; the `WizardView` and `WizardInput` summaries stop naming F6. Verify the Core tests pass.

## 4. Starting always opens the tab (handler)

- [ ] 4.1 Red: `StartSessionHandlerShould` asserts a started, listed session answers `StartedAndOpened` with the tab outcome and an unlisted one `NotListedYet`, with no case for starting without a tab. Green: `StartSession` carries only the plan; `StartSessionHandler` drops the flag; `StartSessionOutcome.Started` goes. Verify the Core tests pass and `EffectDispatcherShould` still finds the registration.
- [ ] 4.2 Red: `WordingOfOutcomesShould` asserts `NotListedYet` says to press Enter on the session once it appears, and the reflection fact still finds words for every `StartSessionOutcome` case. Green: `WordingOfOutcomes`. Verify the Core tests pass.

## 5. The console only quits (ScreenLoop)

- [ ] 5.1 Red: `ScreenLoopShould` asserts the screen is run once and the tab is cleared and 0 returned on `Quit` or no effect, and that any other exit effect throws `UnreachableException`, with no `TerminalEffects` in the loop; the facts that attached twice and returned are removed. Green: `ScreenLoop` without the loop or `TerminalEffects`; `Program.cs` builds `TerminalEffects` only for `here`. Verify the Core tests pass.
- [ ] 5.2 Red: `DiagnosticsPrivacyShould` runs its daily flow with an open-in-tab effect instead of an attach and still finds no fixture value. Green: remove the `HandingOver` and `ChildReturned` events, their lines and their facts in `DiagnosticLineShould` and `DiagnosticLogShould`. Verify the solution's tests pass, including `DiagnosticEventsShould`, `EffectRoutingShould` and `LayeringShould`.
- [ ] 5.3 CLAUDE.md: the effect routing (`ILeavesScreen` is `Quit`, and `AttachToSession` only through `vantage here`), the Terminal.Gui UI-thread fact (why the harness existed), the Windows Terminal facts that name `o` and the title written again after every hand-off, and the open item on attaching from two tabs. Verify no line of CLAUDE.md describes an attach from the console.

## 6. The wizard dialog

- [ ] 6.1 `LaunchWizardDialog` handles F5 only and has the buttons `_Start (F5)` and Cancel; `WizardResult` and `DialogEffects` carry only the plan. Verify `dotnet build -c Debug`, then headless with `drive`: `n`, choose a repository, type a name, F6 leaves the wizard open and starts nothing, F5 starts the throwaway session and opens its tab (stop and remove it afterwards).
- [ ] 6.2 README's wizard section (F5 starts and opens the tab; no F6, no start without a tab) and the `s` row of the worktrees keys. Verify the README matches `Wording.WizardHint`.

## 7. The hand-off check drives vantage here

- [ ] 7.1 Red: `HandoffCaptureShould` asserts a capture of `here` (the hint line, then Claude's banner) shows Claude's screen and one without the banner does not. Green: `HandoffCapture` looks for the banner anywhere in the stripped capture, with no alternate-screen bracketing. Verify `dotnet test tools\HeadlessTerminal.Tests -c Debug`.
- [ ] 7.2 `HandoffCheck` runs `vantage here <throwaway name>` with no prompt through `StepRunner` (a `SmokeStep` waiting for the `Claude Code v` marker, then Ctrl+Z, then the wait for `here` to exit), then stops and removes the job found by name whatever happened; exit codes 0, 1, 2 and 3 as the design says. The headless terminal's README, CLAUDE.md's hand-off check line and the descriptions in `check.yml` and `claude-contract.yml` follow. Verify `dotnet run --project tools\HeadlessTerminal -- check` from `tools\HeadlessTerminal` exits 0 against a Release build, and that `claude agents --json` lists no `vantage-check-` job afterwards; record whether the banner appeared without a prompt.

## 8. Version and checks

- [ ] 8.1 Raise `Version` a minor step above `main`'s when the pull request is opened (0.19.0-preview.1 if `main` is still on 0.18.0). Verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build`, `dotnet format --verify-no-changes` and `openspec validate enter-opens-tab --strict` pass.
- [ ] 8.2 `CONTRIBUTING.md`'s manual checks: F5 in the wizard opens the tab, Enter opens or switches, Ctrl+Z in the tab closes it, and the Vantage tab keeps its title; the checks that attach in the console and return go. Run the smoke check against the packed tool. Verify it exits 0.
- [ ] 8.3 The product owner installs the build with `tools\install-local.ps1` and checks Enter on a session with no tab (opens one), Enter again (switches to it), `o` (nothing), F5 in the wizard (starts and opens), F6 (nothing) and `vantage here` (the new hint, Ctrl+Z back to the shell). Record here.

## Workflow follow-up

- At archive, edit the Purpose lines of `openspec\specs\session-attach\spec.md` ("in the same tab or in a new Windows Terminal tab") and `openspec\specs\console-layout\spec.md` ("across switches and hand-offs"), which a delta cannot change.
- Archive in the pull request that completes the change.
