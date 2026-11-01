# Proposal

## Why

The security review of 1 November 2026 (branch v-sec, at main a09d1184, read-only) found one high finding, three medium ones and a handful of low ones. The high one loses work: a worktree that git calls prunable while its folder still exists (its `.git` file was lost, a remove half-finished, or a copied `.git` folder carries a crafted `gitdir`) is shown as "the folder is gone" with no files counted, and `d`, or deleting its session, deletes the folder with no question, no fresh look at the disc and no check that it lies under the roots; reproduced in scratch repositories, including deleting a folder outside the roots. Alongside it, three ordinary git layouts are shown as leftovers that force-remove deletes, the release package can be swapped before it is published to nuget.org, the release build runs with write and signing rights, and some smaller gaps reopen what the review of 29 October closed.

## What Changes

- **A prunable worktree whose folder exists is a leftover, not a gone folder:** its files are counted, so `d` refuses it while it holds files and force-remove with the typed folder name deletes it; a folder with no files is still deleted and its registration pruned. The facts are read from the disc when the key is pressed, not taken from the row. Deleting a session keeps such a worktree and says why, which reverses the scenario "Delete clears a worktree whose .git has gone" whenever files remain. Deleting a session never reads "git does not list it" as "the folder is gone".
- **Leftovers are deleted only inside the roots:** the last step before a folder is deleted refuses a path that is not under a repository root or the worktree root, with a reason in the status line.
- **A `.git` file that points at an existing folder is never a leftover:** a checkout made with `--separate-git-dir`, a worktree of a bare repository and a worktree made with `--relative-paths` are shown as elsewhere and refused removal even when forced. A relative `gitdir` is resolved against the `.git` file's own folder, never Vantage's current folder.
- **The tab shell is found only through real ancestors:** walking up from a session's process to its Windows Terminal tab stops at a parent younger than its child, so adopt and close-empty never end an unrelated tab's shell under a reused process id.
- **Relative PATH entries are never searched,** for git, gh, wt.exe, pwsh.exe, az.cmd and the VS Code shim, and every tab runs `pwsh.exe` by its full path.
- **A session id from outside Vantage has the CLI's shape:** letters, digits and hyphens, starting with a letter or digit; any other row is skipped, so an id can neither be read as a `claude --resume` option nor reach outside the transcripts folder.
- **The memory breakdown never shows an option's value as a label:** the value of `--from`, `--with`, `--registry` and the like is skipped, and text holding a colon is never a label.
- **A NUL character in a state file reads as a bad value,** not a crash at start-up.
- **CI:** `publish.yml` verifies the release package's provenance attestation before pushing it, and immutable releases are turned on; `release.yml` builds and tests in a job with read rights and attests and publishes in a second job that only downloads; `claude-contract.yml` raises its issue from a job of its own, so the install and the tests run without `issues: write`.
- **Documentation:** README's "What Vantage trusts", `CONTRIBUTING.md` and `.claude\rules\checks.md` match.
- The tool's version takes a minor step in the pull request that implements this: removal refuses more than it did.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `worktree-management`: a prunable worktree whose folder exists is detected and counted as a leftover; leftovers are deleted only inside the roots; elsewhere covers every `.git` file whose target exists.
- `session-attach`: delete keeps a worktree whose `.git` has gone while it holds files; adopt and close-empty find a tab's shell only through real ancestors.
- `external-tools`: relative PATH entries are skipped, and a tab's PowerShell is started by its full path.
- `session-listing`: a session id must have the CLI's shape; the memory breakdown never labels a program with an option's value.
- `settings`: a NUL character in config.json reads as a bad value.

## Impact

- **Domain:** `WorktreeHealth` and `HealthReason` (a prunable folder that exists), `RemovalDecision` and `RemovalReason` (outside the roots), `TerminalTab`, `SessionId`, `ProgramLabel`, `FolderPath.TryParse`.
- **Application:** `WorktreeInventory`, `WorktreeInspector`, `WorktreeRemover`, `RemoveWorktreeHandler`, `DeleteSessionHandler`, `Wording` and its outcome words.
- **Adapters:** `PathFolders`, `AzureCliExecutable`, `EditorLocator`, `WindowsTerminalArguments` and `WindowsTerminal`.
- **Program.cs:** hands the located pwsh.exe to `WindowsTerminal`.
- **CI and repository:** `publish.yml`, `release.yml`, `claude-contract.yml`; the immutable releases setting through `gh api`.
- **Docs:** README, `CONTRIBUTING.md`, `.claude\rules\checks.md`.

## Out of scope

- Multiple Claude accounts, which is a later change.
- Listing a `--separate-git-dir` checkout or a bare repository's worktrees as repositories of their own: they are shown and protected, not counted.
- Following a junction in a configured root to match git's spelling of a path; a leftover listed by git under the real path is refused deletion instead.
- The review's informational items: a repository-local `core.fsmonitor` in a copied `.git` folder, one malformed listing row failing the whole listing, folder names ending in a dot, the repository `git worktree prune` runs in, Enter focusing the first tab with a matching title, `vantage here` writing the folder name unfiltered, the Dependabot bump condition, and re-claiming a tab registration.
- The deferred supply-chain items: `Interop.UIAutomationClient`, the unpinned Claude installer in CI, NuGet lock files, and the owner's ability to merge past `check`.
