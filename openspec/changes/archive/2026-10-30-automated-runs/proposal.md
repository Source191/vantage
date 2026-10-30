# Proposal

## Why

The Sessions table sometimes shows a second row for a session's worktree: no name, the hollow `▯` in TAB, `■ /` in STATE, an age of 0m and a couple of hundred megabytes (the owner's screenshot, 30 October 2026). It is not a session. It is a `claude` process started through the Agent SDK, here the security-guidance plugin's background review, which runs for about 20 seconds after a commit or push in that worktree, or when a turn there ends, and uses tokens. The CLI lists such a process as an interactive session with the worktree's folder and no status, so Vantage shows it as a session the user can neither attach to, adopt nor rename. A session committing after every TDD step makes these rows frequent. They should leave the table, but the memory they use should still be visible.

## What Changes

- **Automated runs leave the table:** an interactive row in `claude agents --json --all` that has no `status` is an automated run (the Agent SDK in any language, `claude -p`), not a session. It is not a row, is not counted in the table's title and takes no keys.
- **The counts show them:** while any run, the Sessions counts end with `◇ 2 automated 431 MB` in grey: how many, and the memory of their process trees measured as the detail pane's Memory line measures a session's. Nothing shows when none run.
- **The rule stays on the documented listing:** the decision uses only the listing's `kind` and `status` fields, never the `~\.claude\sessions` folder, as `openspec\config.yaml` requires. The evidence (below and in the design) goes into CLAUDE.md's CLI facts, and an opt-in contract test pins it against the installed CLI without spending tokens.
- **Version:** one preview step above `main`'s.

Evidence, gathered 30 October 2026 with CLI 2.1.314: the plugin's review and a `claude -p` run both list as `kind: interactive` with no `status` for their whole lives (the `claude -p` row even carries a derived name, so a missing name is not the sign); a real interactive `claude` registers without a status and has `idle` 130 ms later; `claude logs` never registers at all.

Out of scope: naming what started a run (the plugin, a script or a tool), listing runs one by one or a key to show them, a memory breakdown (`m`) for a run, reading the registration files' `entrypoint` field (it would break the project's rule on the sessions folder), changing how worktree removal treats a folder a run is using (git's in-use refusal stands), and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: ADDED "Automated runs are not sessions" (the hidden rows and the start of a real interactive session) and "The counts show the automated runs" (beside the counts requirement tabs-and-key-bar still holds unarchived).

## Impact

- **Ports:** `IClaudeCli.ListSessionsAsync` answers a `SessionListing` (the sessions and the automated runs) instead of a bare list; `LaunchService` and the worktree scan read its sessions alone.
- **Domain:** an `AutomatedRun` record (session id, folder, process id).
- **Adapters:** `ClaudeAgentsListing` sorts an interactive row without a status into the runs; `ClaudeStates` keeps its words.
- **Application:** `SessionRowEnricher` (or a sibling) measures the runs into an `AutomatedRuns` summary; `RowsLoaded` carries it and `SessionsViewState` keeps it; `Wording.SessionCounts` adds the part.
- **Tests:** listing facts from JSON fixtures, enricher and reducer facts with the fake process registry, a `WordingOfScreensShould` fact, and a `ClaudeContract` fact that starts a prompt-less `claude -p` with its standard input redirected and finds it among the runs.
- **Docs:** README's Sessions section and TAB column, the counts in the help where they are described, CLAUDE.md's CLI facts.
