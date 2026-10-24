# Proposal

## Why

An architecture review on 24 October 2026 (`docs\architecture-review.md`) found eight defects in the sessions and worktrees views and the state store that can lose data, misreport, or act on the wrong row. They are independent of the restructuring changes that follow, small enough for one pull request, and the restructure should not carry behaviour fixes inside it.

## What Changes

- The worktrees view decides whether a refusal can be forced from the removal decision itself, not by searching the reason text for "session " or "locked".
- The session row enricher's location cache is safe when two refreshes overlap (the five-second timer and the refresh after every action can coincide).
- The stop effect carries the selected row like every other effect, instead of looking the row up again in a listing that may have refreshed.
- `HasTranscript` on a session row is a required argument, so a forgotten value can no longer read as "safe to resume".
- The configured worktree base is the existing `WorktreeBase` enum rather than a string compared with `"Head"`; the JSON values `RemoteDefault` and `Head` in `config.json` are unchanged.
- Enter on a worktree row selects the running session by its session id, not by its name, because names are not unique.
- The worktree scan stops counting a leftover folder's files at a limit instead of walking its whole tree on every refresh; a refusal then says at least that many files would be lost.
- Recent repositories and session descriptions are stored one record per file, as tab registrations already are, so `vantage here` and the console cannot overwrite each other's writes.

Out of scope: every structural change from the review (project split, generic screen, effect handlers, value objects), multiple Claude accounts, and any new key or column.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `worktree-management`: a refusal that cannot be forced never suggests force-remove; Enter goes to the session by identity; counting a leftover folder's files is bounded.
- `session-attach`: stop acts on the row that was selected when the key was pressed, even if the listing refreshed in between.
- `session-listing`: overlapping refreshes leave the view consistent.
- `session-launch`: records written by two Vantage processes at once are both kept; the recent-repositories scenario says ten, matching the code's long-standing limit, instead of five.

## Impact

- `Domain\RemovalDecision.cs`, `Screens\WorktreesView.cs`, `Screens\SessionsView.cs`, `Screens\SessionRowEnricher.cs`, `Screens\WorktreeInventory.cs`, `Screens\Tui\SessionsScreen.cs`, `Screens\Tui\LaunchWizardDialog.cs`, `Adapters\State\StateStore.cs`, `Adapters\State\VantageConfig.cs`, `Program.cs`.
- State folder layout: `recent.json` and `sessions.json` become folders of one-record files; a one-off read of the old files migrates them.
- No new packages. No CLI surface changes.
