# Design

## Context

`ClaudeAgentsListing.Parse` turns every listing row with a session id and a folder into a `Session`: an `InteractiveSession` for `kind: interactive` with a pid, a `BackgroundSession` otherwise. `ClaudeStates.From` maps a live row with neither status nor state to `DisplayState.Unknown("/")`, which is the `■ /` in the owner's screenshot, and `SessionsScreenDefinition.Swatch` gives every interactive row the hollow `▯`. `IClaudeCli.ListSessionsAsync` answers `IReadOnlyList<Session>` to three callers: `Program.LoadAsync` (then `SessionRowEnricher.EnrichAsync`, then `RowsLoaded`), `Program.ScanAsync` (the worktree scan, which uses the sessions to find a live session in a worktree) and `LaunchService` (name clashes). `Wording.SessionCounts(rows)` builds the counts from the rows alone.

What was measured, 30 October 2026, CLI 2.1.314 (see proposal.md for why):

| Process | Registers | Listing row while it runs |
|---|---|---|
| security-guidance review (Agent SDK, Python) | yes | interactive, pid, folder, no name, no status, about 20 s |
| `claude -p "…"` | yes | interactive, pid, derived name (`tmp-9d`), no status |
| `claude -p` with no prompt, standard input redirected and never written | yes | the same for about 17 s, then exits 1 without calling the model |
| `claude` in a terminal | yes | no status for its first 130 ms, then `idle` |
| `claude logs <id>` | no | none |

The registration files also carry `entrypoint` (`cli`, `sdk-py`, `sdk-ts`, `sdk-cli`, `claude-desktop`), but `openspec\config.yaml` forbids reading the sessions folder, so the design uses the listing alone (the owner's choice).

## Goals / Non-Goals

**Goals:**
- One rule, in the adapter that already reads the CLI's words, decides what is a run.
- The runs' memory reaches the counts through the same process-tree measurement as the detail pane's Memory line.
- Every piece is testable without a real CLI, and one opt-in contract fact pins the rule against the real one without spending tokens.

**Non-Goals:**
- Telling runs apart by what started them.
- Keeping a run's history between refreshes.

## Decisions

- **The rule: `kind: interactive`, a pid and no `status`.** It lives in `ClaudeAgentsListing` beside `ClaudeStates`, the one place the CLI's fields are read. A row without a pid stays a session (a stopped interactive row is not something the listing has been seen to give, and hiding it would hide a fact). A missing name is not part of the rule, because `claude -p` rows carry a derived name. Alternative rejected: the registration's `entrypoint` (precise, but breaks the project rule on the sessions folder).
- **The listing answers a `SessionListing`, not a wider `Session` hierarchy.** `ListSessionsAsync` returns `SessionListing(IReadOnlyList<Session> Sessions, IReadOnlyList<AutomatedRun> AutomatedRuns)`. An `AutomatedRun` subtype of `Session` was rejected: every switch over `Session` (enricher, reducers, effects, the two-case `UnreachableException` arms) would learn a case that can do nothing, and a row could reach a key by accident. `LaunchService` (which reads the listing only to find the job it started) and the worktree scan read `.Sessions`, so a run never counts as a live session in a worktree.
- **`AutomatedRun(SessionId, FolderPath, int ProcessId)` in `Domain`.** The folder and session id are kept although nothing shows them yet, because they cost nothing and a later "what is running" view would need them; no name, no state.
- **Measured with the rows, carried with the rows.** `SessionRowEnricher` takes the listing and also returns an `AutomatedRuns(int Count, MemorySize Memory)` summary from the same process snapshot (`ProcessTree.Under(pid, ...)?.Total`, a gone process adding nothing); `RowsLoaded` gains it; `SessionsViewState.AutomatedRuns` holds it, replaced on each `RowsLoaded` and left as it was on `LoadFailed` (as the rows are). `AutomatedRuns.None` stands where a null would, as `Footer.Blank` does.
- **Words in `Wording`.** `SessionCounts(rows, automated)` appends `◇ {count} automated {Memory(memory)}` in `TextTone.Grey` when the count is above zero; `◇` is unused elsewhere and reads as "not a session" beside the filled state glyphs. The Sessions screen definition passes the state's summary.
- **The contract fact holds a prompt-less `claude -p` open.** Started with standard input redirected and never written, it registers and waits about 17 s for input before giving up; the fact polls `ListSessionsAsync` until its pid is among `AutomatedRuns` and not among `Sessions`, then closes standard input and waits for exit 1. No prompt means no model call. It carries `Category=ClaudeContract` like the others, so it runs nightly and only when `VANTAGE_CLAUDE_CONTRACT=1`. The "real interactive session gains a status" half is a CLAUDE.md fact, not a contract fact: proving it needs a pseudo-console, and the headless terminal's `drive` measured it.

## Risks / Trade-offs

- [A real `claude` is listed in its first 130 ms] → it is counted as a run for one refresh and shows from the next (at most five seconds); a spec scenario records this.
- [A future CLI gives interactive sessions no status, or gives SDK runs one] → the contract fact fails nightly and opens the `claude-cli` issue; the CLAUDE.md fact names the version measured.
- [A worktree can be removed while a run uses it] → git refuses with "Permission denied", which Vantage already reads as `GitException.InUse`; the run is short-lived.
- [The runs' memory is a moment's figure] → the same as a session's Memory line; runs last seconds, so the part may come and go between refreshes, which is the truth.

## Migration Plan

Nothing is stored. Rolling back is installing the earlier release.
