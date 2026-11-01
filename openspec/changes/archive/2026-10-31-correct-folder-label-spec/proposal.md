# Proposal

## Why

The folder-actions requirement "The folder is shown relative to its root" still describes the WHERE column from before the tabbed console: a WHERE cell reading the repository, the place and the branch in brackets. Since the detail pane took the place and the branch, the sessions view's first folder column is REPOSITORY and shows the repository alone, and the worktrees view's WORKTREE cell shows the repository › the folder's slug; no cell has ever been given a branch since. The null-free-rows survey found the mismatch, and the owner chose on 31 October 2026 to correct the spec to the code (and to drop `WhereLabel`'s unused branch in null-free-rows) rather than add a branch to the cells.

## What Changes

- **MODIFIED** `folder-actions`, "The folder is shown relative to its root": the description names the REPOSITORY and WORKTREE cells and what each reads, says neither shows the branch, and keeps the rule for a folder's label; the scenarios read the REPOSITORY and WORKTREE cells as the console draws them today, and gain one for a worktree session and one for the main checkout.
- **`CONTRIBUTING.md`:** the manual check that says a new session's "WHERE cell reads the repository › the worktree's slug" reads its REPOSITORY cell (the repository) and the worktree's WORKTREE cell (the repository › the slug).
- No code changes and no `Version` raise: the spec is brought to the behaviour already on main.

Out of scope: removing `WhereLabel`'s branch from the code, which is null-free-rows; the stale settings-rows requirement in `session-listing`, which is owed a change of its own; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `folder-actions`: the requirement "The folder is shown relative to its root" describes the REPOSITORY and WORKTREE cells as they are, with no branch.

## Impact

- **Specs:** `openspec\specs\folder-actions\spec.md` on archive.
- **Docs:** `CONTRIBUTING.md` (one manual check).
- **Code:** none.

## Order

Independent of the null programme and documentation only, so it can merge at any point; it pairs with null-free-rows (change 4), which removes the branch the old wording described.
