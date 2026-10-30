# Proposal

## Why

The tables carry more than a row can hold: on the Sessions tab WHERE packs the repository, the worktree and the branch into one cell, and on the Worktrees tab eight columns leave the WORKTREE cell eliding names such as `MK.Sales…mmission`. Several facts are never shown at all: why a session waits (the listing's waitingFor is read and dropped), when it was last prompted, its session id, a worktree's merge state and what its unpushed commits and changes are. The UI review's option 4, confirmed on 30 October 2026, puts a pane under the table that shows the selected row in full, so the table can keep only what is worth scanning down.

The first version also showed the bottom of the session's screen from `claude logs`. The owner's terminal check on 30 October 2026 found that four lines of screen were too few to read a question and that the facts repeated the table, and decided: facts only, no `claude logs`, and fewer columns in both tables.

## What Changes

- **A facts pane under the Sessions and Worktrees tables** (the Config tab has none), under a grey divider titled with the selected row's name, exactly as tall as its tab's facts can be, so the table never jumps between rows.
- **The Sessions table drops WHERE for REPOSITORY:** the repository's name, or the folder's short label outside a repository; NAME, TAB, STATE, REPOSITORY, PR, AGE. The pane shows what the row does not: what the session is waiting for (the listing's reason, a first prompt, or nothing), when it started, when Vantage last saw it prompted, the full folder, the session id, the worktree (its slug or path inside the repository, the main checkout, or not in a repository), the branch, its tab, the folder's changed files, unpushed commits and last commit, and its memory.
- **The MEM column goes;** memory shows only in the pane, with `m` for the breakdown.
- **The Worktrees table keeps WORKTREE, LAST COMMIT and SESSION;** BRANCH, PR, DIRTY, UNPUSHED and HEALTH move to the pane, and rows keep their health colours. The pane shows the health with its reason, the branch and whether it is merged into origin's default branch, the pull request, the full folder, whether and when Vantage created it, the changed files and unpushed commits, then the unpushed commits (hash, age, subject) or, when none, the changed files, up to five and how many more.
- **The help explains the pane's lines,** health words included, as it explained the columns that moved.
- **Detail never delays the listing:** it is read off the UI thread for the selected row only, when the selection moves and after each refresh, and a late answer for a row the user has left is dropped.
- **The waiting spinner stays in the table** above the pane, whatever the pane's height.
- **Version:** one preview step above `main`'s.

Out of scope: the session's screen or output (the `claude logs` spike is recorded in the tasks and design and nothing of it ships), a draggable divider, reading the transcript for the last prompt (config.yaml allows only an existence check), the worktrees footer's health sentence (it stays), and multiple Claude accounts.

## Capabilities

### New Capabilities

- `detail-pane`: what the pane under the Sessions and Worktrees tables shows for the selected row, how tall it is, and that it never delays the listing.

### Modified Capabilities

- `session-listing`: "Rows show repository, branch and worktree" (REPOSITORY in the table, the rest in the pane), "Long names never hide columns" (REPOSITORY is elastic), "Rows show the session's memory" (the pane only).
- `worktree-management`: "One WORKTREE column names the worktree" (no BRANCH column), "Each row shows work at risk" and "Locked worktrees are marked" (in the pane), "The worktrees header and help match the sessions view" (the help explains the pane).

## Impact

- **Domain:** `Session` gains the listing's waiting reason.
- **Ports:** `IGit.UnpushedCommitsAsync(worktree, limit)` (hash, age, subject) and `ChangedFilesAsync(worktree, limit)`.
- **Application:** `Detail\SessionDetail` and `WorktreeDetail`, `DetailLoader`, `DetailRequests` (the order of requests and which answer shows), `DetailLayout` (the facts in three columns at the pane's width), `IDetailSource` with `SessionDetails` and `WorktreeDetails`, `SessionActivity` (the last prompted time, in memory), `Wording` (the pane's lines and help, the new columns).
- **Adapters:** `ClaudeAgentsListing` (waitingFor), `GitCli` (the two listings).
- **Tui:** `DetailPane` under each `TableBody` that has a detail source, the definitions' new columns, the waiting spinner centred on the table, the tabs disposed under the binding guard.
- **Tests:** `SessionActivityShould`, `DetailLoaderShould`, `DetailRequestsShould`, `DetailLayoutShould`, `DetailSourcesShould`, `WordingOfDetailShould`, `WordingOfKeysAndColumnsShould`, `ClaudeAgentsListingShould`, `GitCliIntegrationShould`; headless captures at 120 x 36.
- **Docs:** README (the pane, the new columns of both tables), CLAUDE.md (the pane in the architecture paragraph).
- **Release:** a preview raise; the owner's terminal check.

## Order

The third of the UI review's changes to be applied, after where-column, tabs-and-key-bar and config-tab (the owner chose it before wizard-one-form, 30 October 2026). Its MODIFIED blocks are based on those changes' text where they changed a requirement (where-column's WHERE and WORKTREE requirements, config-tab's "Long names never hide columns", tabs-and-key-bar's worktrees help), so it archives after them. The smoke script finds each view by the first column only it has (NAME, WORKTREE), so it is unchanged.
