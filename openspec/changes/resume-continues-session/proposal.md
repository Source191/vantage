# Proposal

## Why

Pressing `R` leaves duplicate rows: every resumed session appears twice, once as the old stopped or failed job and once as a new running one with the same name, and a session resumed twice appears three times. `claude --bg --resume <sessionId>` on a job that is still listed starts a copy under a new session id rather than continuing the job (seen on 1 Nov 2026 with CLI 2.1.316: v-keys `3e57bf3a` stayed listed as failed beside new jobs `7a9f6d30` and `c429a96e`). Rename already avoids this by removing the job first. While a resumed job starts up it also shows the meaningless state "/blocked".

## What Changes

- `R` removes each chosen job with `claude rm <jobId>` before `claude --bg --resume <sessionId>`, so the CLI continues the same session id, its row comes back to life in place, and a Vantage tab registered for the session still matches it.
- A resume that fails after the job was removed leaves the session unlisted with its transcript still on disc, so the status line names the session, says why, and gives `claude --bg --resume <sessionId>` to bring it back, as rename does when its restore fails.
- A background job that has a process but no status yet (the moment after a start or resume) shows as "starting" in the working colour instead of the raw "/blocked", and the header counts it with the working sessions.
- Patch release: 0.19.0-preview.15.

Out of scope: tidying the duplicates already listed (each is a separate session with its own transcript, and `d` removes the old stopped row), multiple Claude accounts (a later change), and the reboot spike 0.3 (`respawn --all`), which this change does not settle.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: resume-all continues each session by removing its job first, and reports a failed resume with the command to bring the session back.
- `session-listing`: a background session with a process and no status yet shows as starting, counted with working in the header.

## Impact

- `src\Source191.Vantage.Core\Application\Actions\ResumeSessionsHandler.cs` and its `ResumeNote` cases; `WordingOfOutcomes` for the new words.
- `src\Source191.Vantage.Core\Domain\DisplayState.cs` (a `Starting` value of kind Working), `src\Source191.Vantage.Infrastructure\Claude\ClaudeStates.cs`, and `WordingOfConsole.CountedState` for the header word.
- Tests: `ResumeSessionsHandlerShould`, `WordingOfOutcomesShould`, `ClaudeStatesShould`, the header counts tests.
- `README.md` (the after-a-reboot paragraph), `CLAUDE.md` (the resume CLI fact), `src\Source191.Vantage\Source191.Vantage.csproj` version.
