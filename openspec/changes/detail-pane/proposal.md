# Proposal

## Why

With six sessions the table has rows to spare and the one thing the native agent view showed that Vantage does not is why a session waits: the listing's waitingFor field is read and dropped, and since the `a` key went (#95) nothing in Vantage shows it. A footer has one line for a worktree's health reason or a refusal, and the memory figure sits in a column that costs WHERE seven cells on the laptop. The UI review's option 4, confirmed on 30 October 2026, spends the recovered rows on a pane under the table that shows the selected row in full, including its recent output from `claude logs`, and takes the memory figure from the MEM column.

## What Changes

- **A detail pane under the Sessions and Worktrees tables** (the Config tab, which arrived after this proposal, has none), under a divider that can be dragged, nine rows by default at 36 rows, titled with the selected row's name.
- **For a session:** its state with the reason it waits (the listing's waitingFor, or that no prompt has been sent), when it started, when it was last prompted (the last time Vantage saw it leave needs input or idle for working, while Vantage was running; otherwise that it was not seen), its session id, the full folder, the branch, the pull request with the key that opens it, its tab (open and the tab's colour, or its own terminal), the worktree's changed files, unpushed commits and last commit from git, its memory with the key for the breakdown, and the last lines of its terminal output from `claude logs`, refreshed with the rows for the selected row only.
- **For a worktree:** its health with the reason in full, whether and when Vantage created it, the full folder, the branch and whether it is merged into origin's default branch, the pull request, the risk line, the session inside it with its state and the key that goes to it, and its unpushed commits (hash, age, subject, as many as fit, then how many more) or, when none, its changed files.
- **`claude logs` joins the CLI surface Vantage depends on:** in config.yaml, the Claude port, the nightly contract test and CLAUDE.md's facts, after a spike that measures its cost and shape. Its output is drawn and discarded: never recorded in the diagnostic log, never part of a problem report.
- **The MEM column goes;** WHERE takes its cells (64 at 120 columns, so the 61-character fixture name shows whole on the laptop); `m` and the pane carry the figure.
- **Detail never delays the listing:** the pane is loaded off the UI thread for the selected row when the selection changes and after each refresh, and the rows never wait for it.
- **Version:** one preview step above `main`'s.

Out of scope: a live cockpit or wall of panes (options 11 and 13), reading the transcript's write time as a fallback for the last prompt (config.yaml allows only an existence check; the owner can widen that rule in a change of its own), the worktrees footer's health sentence (it stays), warming details for unselected rows, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

- `detail-pane`: what the pane under each table shows for the selected session or worktree, how its recent output is obtained and kept off the record, how it is sized, and that it never delays the listing.

### Modified Capabilities

- `session-listing`: "Rows show the session's memory" moves the figure to the pane and drops the column; "Long names never hide columns" (as config-tab leaves it) gives WHERE 64 cells at 120.

## Impact

- **Domain:** `Session` gains the listing's waiting reason; a new `AnsiText.Strip` for terminal captures (the headless harness's `EscapeSequences` rule, which the harness can then take from Core).
- **Ports:** `IClaudeCli.LogsAsync(jobId)`; `IGit.UnpushedCommitsAsync(worktree, limit)` (hash, age, subject) and `ChangedFilesAsync(worktree, limit)`.
- **Application:** `Detail\SessionDetail` and `WorktreeDetail` records, `DetailLoader` (git, logs, the created-worktree records, the merge check, for one row), `SessionActivity` (the last prompted time, observed beside `StateTransitionNotifier`), `Wording` (the pane's lines, in `WordingOfDetail`), `WordingOfScreens` (the columns without MEM), `Diagnostics\ChildCommandWords` ("logs").
- **Adapters:** `ClaudeCli` (logs), `ClaudeAgentsListing` (waitingFor), `GitCli` (the two listings).
- **Tui:** `DetailPane` (a resizable frame under each `TableBody`), `ConsoleScreen` (loading the selected row's detail with a sequence so a stale answer is dropped), `SessionsScreenDefinition` (MEM gone).
- **Tests:** `AnsiTextShould`, `SessionActivityShould`, `DetailLoaderShould`, `WordingOfDetailShould`, `ClaudeAgentsListingShould` (waitingFor), `ClaudeCliShould` (logs), `GitCliShould` (the listings, against real git), the contract test (`claude logs` on the started session), `DiagnosticsPrivacyShould` (a logs fixture never reaches the output), `ChildCommandShould`; headless captures at 120 x 36.
- **Docs:** README (a pane section, the sessions columns without MEM, the memory key's words), CLAUDE.md (the `claude logs` fact, the pane in the architecture paragraph, the diagnostics rule naming logs), config.yaml (the CLI surface), the two specs.
- **Release:** a preview raise; the contract tests run locally with `VANTAGE_CLAUDE_CONTRACT=1`; the owner's terminal check of the pane against a session at a permission prompt.

## Order

The third of the UI review's changes to be applied, after where-column, tabs-and-key-bar and config-tab (the owner chose it before wizard-one-form, 30 October 2026). Its MODIFIED block for "Long names never hide columns" is based on config-tab's text (`<` and `>` scroll); "Rows show the session's memory" on the main spec's. The smoke script's sessions marker is the first column unique to the sessions table, NAME, before and after MEM goes, so the script is unchanged.
