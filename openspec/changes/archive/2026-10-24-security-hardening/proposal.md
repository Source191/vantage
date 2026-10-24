# Proposal

## Why

An Aikido scan flagged two issue groups: `actions/checkout` persisting the GitHub token (two steps here, `check.yml` line 60 and `release.yml` line 20, and two more in another repository), and "path traversal" in ten file calls. Reading the code, the checkout finding is real and cheap to close, most traversal findings are false positives in developer tools, and one points at a genuine bug of a different kind: deleting a leftover worktree folder that contains a junction (pnpm's `node_modules` uses them) follows the junction while clearing read-only attributes, so it changes files outside the folder, and then fails with "Access to the path is denied", leaving the folder half deleted.

## What Changes

- Every `actions/checkout` step in `check.yml` and `release.yml` sets `persist-credentials: false`; no step pushes with git, and the release uses `gh` with its own `GH_TOKEN`. `check.yml` declares `permissions: contents: read` at the top, so its jobs (including `handoff`, which installs and runs Claude Code in the checkout) get a read-only token.
- Deleting a leftover folder never follows a junction or symbolic link: every link inside the tree is removed as a link, never descended into or modified, and the rest of the tree is then deleted as before. A leftover folder holding a junction is deleted completely instead of failing part way.
- The `.worktreeinclude` copy is pinned by tests that feed it git output naming parent-relative, rooted and UNC paths and prove nothing is copied outside the worktree. The glob matcher already discards such paths, so there is no production change; the tests stop a future matcher swap from regressing it.
- The remaining Aikido findings (`MarkerCount`, `ScreenReplay`, `HandoffCheck` in `tools\HeadlessTerminal`) are developer tools whose paths come from the local user's own command line; they are marked as false positives in Aikido, with no code change.

Out of scope: multiple Claude accounts (a later change); the matching checkout findings in `klr.orthopaedics.claude` (`check.yml` line 91, `release.yml` line 19), which need the same edit in a pull request in that repository; the bounded file count for leftover folders, which also follows junctions but only reads; the duplicate `TemporaryGitRoot.DeleteTree` test helper; pinning actions to commit SHAs; and any change to the `tools\HeadlessTerminal` code.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `worktree-management`: adds "Deleting a leftover folder never follows links" beside the existing leftover-deletion requirement, which is unchanged.
- `session-launch`: adds "Copied local files stay inside the new worktree" beside the existing `.worktreeinclude` requirement, which is unchanged.

## Impact

- `.github\workflows\check.yml`, `.github\workflows\release.yml`.
- `src\Source191.Vantage.Infrastructure\Files\FolderDeletion.cs` and a new `FolderDeletionShould` in `test\Source191.Vantage.Infrastructure.Tests`.
- `test\Source191.Vantage.Infrastructure.Tests\WorktreeIncludeCopierShould.cs` (tests only).
- No new packages, no change to keys, state files or the `vantage here` command line: a patch-level fix when next released.
