# Proposal

## Why

`R` resumes failed sessions at once but offers stopped ones in a checklist with nothing ticked, so after a reboot the user ticks every session one by one; the checklist's caption is also cut off at the dialogue's edge, so the reason for the checklist is never read. Once resumed, every session is still in the background and needs Enter pressed on it to see it. A session that was stopped on purpose and is not wanted back is better deleted with `d`, and Enter already resumes a single stopped session in its own tab, so `R` only needs to do the whole list in one press.

## What Changes

- **BREAKING (pre-1.0, minor):** `R` no longer opens a dialogue. It resumes every stopped and failed background session the table shows (so a `/` filter narrows it) under the supervisor, in the table's order, and opens a Vantage tab for each one that resumes.
- With no stopped or failed session shown, `R` changes nothing and the status line says there is nothing to resume.
- A session never sent a message is still skipped and named in the status line, as now; a resume the CLI refuses is named with the CLI's reason and gets no tab.
- The status line says how many resumed and that each opened in its own tab.
- The resume checklist dialogue goes, with its tab word (Vantage (Resume)).
- The key's help tip and the README say what `R` now does.

Out of scope: a confirmation before resuming, remembering or configuring whether tabs open, resuming the selected row only (Enter does that), plain `claude` terminal sessions (they are never stopped or failed rows), and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: the resume requirement is replaced (removed, and a new one added: every shown stopped or failed session, each in its own tab, no checklist), and the dialogue-tab-word scenario loses Vantage (Resume).

## Impact

- `Application\Views\SessionsView` (the `R` case raises `ResumeSessions` or a status message), `ResumeStoppedSessions` and `ResumePlan` removed, `StatusMessage.NothingChosenToResume` renamed `NothingToResume`.
- `Application\Actions\ResumeSessionsHandler` takes `OpenTabHandler` and opens a tab per resumed session; `Composition.Handlers` passes it.
- `Application\WordingOfOutcomes`, `Wording`, `WordingOfScreens` (`DialogName.Resume` removed), `Tui\DialogEffects` and `Tui\ResumeChecklistDialog` (deleted).
- README key table, "After a reboot" paragraph; `Version` raised.
