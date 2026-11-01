# Design

## Context

`R` raises `ResumeStoppedSessions(VisibleRows)`, an `IDialogEffect`. `Tui\DialogEffects.ChooseSessionsToResume` splits the rows with `ResumeSessionsHandler.Plan` into failed (resumed at once) and stopped (offered by `Tui\ResumeChecklistDialog`), then dispatches the background effect `ResumeSessions(sessions)`. `ResumeSessionsHandler` checks each transcript, runs `claude --bg --resume <id> [-n <name>]` and returns `ResumeSessionsOutcome(Resumed, Chosen, Notes)`. The checklist's caption and title are literals in `Tui`, the one place this flow breaks the rule that user text lives in `Wording`. `RenameSessionHandler` already resumes a job and then calls `OpenTabHandler.OpenAsync`, so resume-then-attach in a new tab is a proven sequence.

## Goals / Non-Goals

**Goals:**
- `R` is a plain key: reducer to background effect to handler, with no dialog in between.
- The choice of sessions is a pure, tested rule in the reducer; the tab opening is tested in the handler.

**Non-Goals:**
- Any change to how a single session resumes (Enter, rename, adopt).
- Reordering, throttling or grouping the tabs that open.

## Decisions

- **The reducer chooses the sessions.** `SessionsView`'s `R` case keeps the `VisibleRows` whose session is a `BackgroundSession` in `DisplayStateKind.Stopped` or `Failed`, in table order, and raises `ResumeSessions` with them; with none it returns `StatusMessage.NothingToResume` (the renamed `NothingChosenToResume`, same words "Nothing to resume.", same Hint tone). `ResumeStoppedSessions`, `ResumePlan` and `ResumeSessionsHandler.Plan` go. Alternative: keep the effect and let the handler filter rows; rejected because the reducer already owns the visible rows and a dialog-free effect needs no row list.
- **The handler opens the tabs.** `ResumeSessionsHandler` takes the composition's `OpenTabHandler` (as `RenameSessionHandler` does) and, straight after each resume the CLI continues, calls `OpenAsync(session)`, so tabs open in table order and each attach starts only once its job is running again. A refused resume or a session with no transcript gets no tab. Alternative: open the tabs only and let `claude attach` resume each session; rejected because the status line would lose the per-session result and the CLI's reason for a refusal.
- **The outcome stays one record.** `ResumeSessionsOutcome` keeps `Resumed`, `Chosen` and `Notes`; every resumed session gets a tab, so the wording says so ("Resumed 3 of 3, each in its own tab.") without a separate count. A tab that fails to open throws out of `IWindowsTerminal`, which the dispatcher already reports as a failed effect.
- **The dialog's remains go.** `Tui\ResumeChecklistDialog`, its `DialogEffects` entry and `DialogName.Resume` (with its word in `WordingOfScreens` and its fact in `WordingOfScreensShould`) are deleted; `EffectRoutingShould` and `DialogEffectsShould` then confirm `ResumeSessions` is routed as a background effect.
- **Version.** A minor behaviour change to an existing key; while pre-1.0 the next preview of the current line, `0.20.0-preview.3`, stacked on `resume-continues-session` (0.20.0-preview.2).

## Risks / Trade-offs

- [A session stopped on purpose comes back with a tab] → The help tip and README say `R` resumes every shown stopped session and that `d` removes one for good; `/` narrows `R` to what the filter shows.
- [Many tabs at once, for example ten after a reboot] → Matches the 3 to 10 sessions in daily use; each tab is a normal attach the user can detach from or close.
- [Two attaches to one session if a live Vantage tab already shows a stopped session] → `OpenAsync` forgets any earlier registration for the session first, and a stopped session's attach tab has already closed itself (an attach ends when the session stops), so in practice none is live.
