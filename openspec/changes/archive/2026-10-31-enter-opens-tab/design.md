# Design

## Context

Enter on the Sessions tab raises `AttachToSession`, an `ILeavesScreen` effect: `TableBody` stops the application, `ScreenLoop` runs it through `TerminalEffects` (print `Wording.ReturnHint`, `claude attach`, erase the hint), records `HandingOver` and `ChildReturned`, recovers the console, writes Vantage's tab title and colour again and runs the screen once more with the `ConsoleState` it was handed back. `o` raises `OpenTabForSession(session, HasLiveTab)`, handled by `OpenTabHandler` (open a tab, or switch to a live one through UI Automation). `SessionsView.SessionAction` takes an `Attachment` (`None`, `InOwnTab`, `Here`); `Here` is what refuses an attach while a live tab shows the session (`Footer.OpenInAnotherTab`).

The wizard carries an open-in-tab flag end to end: F5 or F6 in `LaunchWizardDialog` becomes `WizardInput.Start(OpenInTab)`, `WizardEffect.Launch(Plan, OpenInTab)`, `WizardResult`, `StartSession(Plan, OpenInTab)`, and `StartSessionHandler` answers `Started` when no tab was asked for. Enter in a fixed folder's name already starts with the flag set.

`vantage here` (`HereCommandRunner`) is the other user of `TerminalEffects` and `AttachToSession`, and the hand-off check (`tools\HeadlessTerminal\HandoffCheck`) drives the console's Enter attach.

## Goals / Non-Goals

**Goals:**
- One path into a session from the console, Enter through `OpenTabForSession`, and one way to start from the wizard, with no flag left that can only ever be true.
- Remove what only the console's attach needed, rather than leave it unreachable.

**Non-Goals:**
- Reshaping `IConsoleScreen.Run` and `ConsoleExit`: the screen still starts from a `ConsoleState` and hands one back, which nothing reads after this change. Folding that away touches the screen, the scripted test double and every `ScreenLoopShould` fact for no behaviour, so it is left for a tidy of its own.
- Any change to `OpenTabHandler`, the tab script, tab registrations or UI Automation switching: Enter reuses them as they are.

## Decisions

- **Enter raises `OpenTabForSession` with `Attachment.InOwnTab`; the `o` arm goes.** Every refusal `o` had (no selection, a terminal session, a session never sent a message with no process) now applies to Enter, and the live-tab case switches instead of refusing. `Attachment.Here` and `Footer.OpenInAnotherTab` (with its words) are deleted, leaving `Attachment` as `None` and `InOwnTab`. Alternative: keep `o` as an alias; rejected, the owner wants one key and the bar has no room to spare.
- **`AttachToSession` stays, raised only by `vantage here`.** It keeps `ILeavesScreen`, so `EffectRoutingShould` (every marked effect is `Quit` or run by `TerminalEffects`) and `EffectDispatcherShould` (every unmarked effect has a handler) hold without a special case. Alternative: move it out of `Views` beside `TerminalEffects`; rejected as churn with no behaviour.
- **`ScreenLoop` runs the screen once.** It claims and labels its tab, starts the update check, runs the screen, clears its tab and returns 0 on `Quit` or no effect; any other exit effect throws `UnreachableException`, because `AttachToSession` keeps its marker and a silent quit would hide a reducer that raised it. It no longer takes `TerminalEffects`; `Program.cs` still builds one for `here`. The `HandingOver` and `ChildReturned` diagnostic events and their lines go, because nothing records them. Alternative: keep the loop for a future hand-off; rejected under YAGNI, and git keeps it.
- **The wizard's flag goes everywhere.** `WizardInput.Start` and `WizardEffect.Launch` lose `OpenInTab`, `WizardResult` and `StartSession` carry only the plan, and `StartSessionHandler` answers `NotListedYet` or `StartedAndOpened`; `StartSessionOutcome.Started` and its words go. `LaunchWizardDialog` handles F5 only and has the buttons `_Start (F5)` and Cancel. Alternative: keep the flag and always pass true; rejected, a flag with one value is a lie in the types.
- **Words.** Enter's `KeyHelp` reads hint `Open`, line "open in its own tab, or switch to it" and the tooltip `o` had; the `o` line goes and both background-session key bars lose it. The wizard hints read "Enter or Tab next field   F5 start and open in tab   Esc cancel" and, on a fixed folder, "Enter or F5 starts and opens in tab   Esc cancel". `NotListedYet` says to press Enter on the session once it appears. `Wording.ReturnHint` becomes "Ctrl+Z detaches; the session keeps running.", matching `here`'s command-line description.
- **The hand-off check drives `vantage here`.** `HandoffCheck` runs `vantage here <throwaway name>` with no prompt in the repository through the pseudo-console, waits for Claude's banner (`Claude Code v`) in the capture rather than a fixed delay through the existing `StepRunner` and a `SmokeStep` marker, sends Ctrl+Z, and waits for `here` to exit by itself; then it finds the job by name and stops and removes it whatever happened. Exit codes: 0 passed, 1 `here` had to be killed after the detach, 2 the check could not start, 3 Claude's screen never appeared. `HandoffCapture` looks for the banner anywhere in the stripped capture: `here` has no alternate screen of its own to bracket it and prints nothing containing that text, so anchoring on the hint line would only tie the harness to `Wording.ReturnHint`. Starting without a prompt means no model call, as the contract facts already do with `--bg -n`. Alternative: keep starting the session separately with a prompt and attach with `here`; rejected, `here` cannot attach to an existing session.
- **CI.** The `handoff` job and `claude-contract.yml` keep calling `check`; only their descriptions change where they say "attach from the console".

## Risks / Trade-offs

- [Enter no longer works outside Windows Terminal, where an attach in place used to] → Windows Terminal is already a requirement (`openspec\config.yaml`), and a missing `wt.exe` gives the existing footer naming it.
- [Muscle memory: Enter used to attach here] → the release notes and README say Enter opens the tab, and Ctrl+Z in that tab closes it, so the old reflex still ends somewhere sensible.
- [An attach that blocks the UI thread can no longer be caught] → it can no longer happen: the console never awaits a child that owns its terminal.
- [Claude may not draw its banner for a session with no messages] → the check then fails with exit 3 on the first run; the fix is to pass `-p` with the old one-word prompt, which costs a model call, and record which in CLAUDE.md.

## Migration Plan

A minor release; nothing on disc changes. State files, settings and tab registrations are read as before. Rolling back is installing the previous version.

## Open Questions

- Whether `here` with no prompt shows Claude's banner in the pseudo-console (see the last risk); the first run of the re-pointed check answers it.
