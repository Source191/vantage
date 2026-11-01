# Design

## Context

See proposal.md for the problem. `ResumeSessionsHandler` calls `IClaudeCli.ResumeBackgroundAsync` for each chosen `BackgroundSession` without removing its job. `RenameSessionHandler` already removes first (`claude rm <jobId>`, then `claude --bg --resume <sessionId> -n <name>`) and that keeps the session id. `ClaudeStates.From(pid, status, state)` has no arm for a process with no status, so a job that has just been resumed falls through to `Unknown("{status}/{state}")`, which gives "/blocked". `ClaudeAgentsListing` takes interactive rows with a pid and no status out as automated runs before `ClaudeStates` sees them, so only background rows reach that case.

## Goals / Non-Goals

**Goals:** one row per session after `R`; a failed resume never loses a session without telling the user how to get it back; a starting job reads as starting.

**Non-Goals:** removing duplicates the old behaviour already made; changing rename, adopt or delete; a new `DisplayStateKind`.

## Decisions

- **Remove, then resume, per session in the handler.** It mirrors `RenameSessionHandler`, which uses the same CLI behaviour that CLAUDE.md records. The other option was resuming without `--bg` flags so the job "keeps its own saved options", but `--bg` is needed to put the session under supervision, and every flagged form we have seen starts a copy. A stopped or failed job has no process, so removing it ends nothing.
- **No restore attempt on failure.** Rename retries under the old name because the failed step was the new name. For resume, the retry would be the same command that just failed, so the handler reports it. `ResumeNote.Failed` gains the `SessionId`, and its words become `"{name}: {failure}; resume it with claude --bg --resume {sessionId}"`, matching rename's `Unsupervised` wording. `ResumeNote` stays a two-case hierarchy (`NeverSentAMessage`, `Failed`), so its discard arm still throws `UnreachableException`. No new case is needed, because after a removal every failure means the session is unlisted.
- **The CLI's "note:" line stays a failure.** `ClaudeCli.ResumeAsync` treats a `note:` line as "started a copy" (`Continued: false`). After a removal that should not happen; if it does, the user still gets the session id and the command, which is the right advice.
- **Starting is a label, not a kind.** `DisplayState.Starting = new(DisplayStateKind.Working, "starting")` gets the working colour, glyph, sort order and key bar without touching any switch over `DisplayStateKind`. `ClaudeStates` gains `(_, null, _) => DisplayState.Starting` after the no-process arms. The words live in `Domain\DisplayState` alongside the other state labels.
- **The header names the kind.** `WordingOfConsole.CountedState` takes the word from the first row of each group, so a starting row first would make the count read "2 starting". The Working arm uses `DisplayState.Working.Label` instead.

## Risks / Trade-offs

- [A removal succeeds and the resume fails, so the session is no longer listed] → the status line gives the exact command; the transcript stays on disc, as with rename.
- [`claude rm` fails silently (the port returns no result)] → the resume then makes a copy as it does today, so this is no worse than now; the next refresh shows it.
- [A future CLI gives starting jobs a status word] → the new arm simply stops matching.
