# Design

## Context

Aikido raised four "checkout persists credentials" findings and ten "path traversal" findings (five open). What the code does today, checked on 24 October 2026 with .NET 10.0.402:

- **Workflows:** `check.yml` has two `actions/checkout@v4` steps and no `permissions` block; `release.yml` has one, with `contents: write`. No step runs `git push`; the release publishes with `gh release create` using `GH_TOKEN`, and its `git diff` / `git show HEAD^` calls are local.
- **`FolderDeletion.DeleteTree`** clears read-only attributes with `Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)`, then calls `Directory.Delete(folder, recursive: true)`. Reproduced with a junction inside the tree pointing at a folder holding a read-only file: the enumeration walks through the junction and clears the outside file's read-only flag, and `Directory.Delete` then throws "Access to the path 'link' is denied" after removing the link, leaving the tree in place. Its only caller is `Folders.DeleteTree`, used by `WorktreeActions` for orphan, broken and empty rows.
- **`WorktreeIncludeCopier`** builds source and target with `Path.Combine` over paths from `git ls-files --others --ignored --exclude-standard`, filtered through `Microsoft.Extensions.FileSystemGlobbing.Matcher`. Checked against 10.0.13, the version the Infrastructure project references: with patterns `**/*`, `*`, `**/outside.txt` and `../outside.txt`, the in-memory `Match` returns none of `../outside.txt`, `sub/../../outside.txt`, `C:/Temp/x.txt`, `/Temp/x.txt` or `//server/share/x.txt`.
- **`tools\HeadlessTerminal`** reads and moves files at paths from its own command line or current folder; it runs only on a developer machine or the CI runner.

## Goals / Non-Goals

**Goals:** no persisted token in any workflow and a read-only token for `check.yml`; leftover-folder deletion that never touches anything outside the folder and succeeds when the folder holds links; tests that pin the copier's containment.

**Non-Goals:** a containment guard in the copier (unreachable, so untestable red-first, and the repository allows no untested code); changes to `tools\HeadlessTerminal`; the bounded leftover file count (`Folders.Files`), which also walks through junctions but only reads; SHA-pinning actions; the duplicate `TemporaryGitRoot.DeleteTree` helper.

## Decisions

- **`persist-credentials: false` on all three checkouts, `permissions: contents: read` at the top of `check.yml`.** `release.yml` keeps its job-level `contents: write` because `gh release create` needs it, and that token reaches `gh` through `GH_TOKEN`, not git. The `handoff` job is the main beneficiary: it installs Claude Code by piping a downloaded script into `iex` and runs a live session in the checkout. Alternative rejected: leaving the checkouts as they are and ignoring the finding, which keeps a token in `.git\config` for the life of every job.
- **`DeleteTree` walks the tree itself, one level at a time.** For each entry: a reparse point (junction, directory symlink or file symlink, identified by `FileAttributes.ReparsePoint`) is removed as a link (`Directory.Delete(path)` non-recursive for a directory link, `File.Delete` for a file link) and never entered; a plain directory is recursed into; a plain file has its attributes set to `Normal`. Then `Directory.Delete(folder, recursive: true)` removes what is left, which by then holds no links and no read-only files. Removing a junction with a non-recursive `Directory.Delete` was verified to unlink it and leave the target and its read-only file untouched. Alternatives rejected: `EnumerationOptions { AttributesToSkip = ReparsePoint }` alone, which stops the attribute change outside but still leaves `Directory.Delete` failing on the junction; a `FileSystemEnumerable` with a custom recurse predicate, which is more machinery than a short recursive method. The folder's own attributes are checked before anything is enumerated, so a folder that is itself a junction is removed as a link and its target is never entered; the rule has no exception.
- **The copier gets tests, not code.** Two facts in `WorktreeIncludeCopierShould`, using a `GitCli` over the shared `FakeProcessRunner` answering `ls-files` with a crafted listing, a real temporary repository folder for the `.worktreeinclude` file and an include pattern of `*` with `**/*`. They pass on first run: they are characterisation tests, written to fail if the matcher is ever replaced by one that keeps parent-relative or rooted paths. The fact that they pass today is the evidence for marking the Aikido finding a false positive.
- **Junctions in tests come from `cmd /c mklink /J`** run through the real `ProcessRunner`, because .NET has no junction API and junctions, unlike symbolic links, need neither elevation nor Developer Mode. File symbolic links are covered by the same attribute rule but not by a test, for the same privilege reason.
- **Aikido triage.** `MarkerCount`, `ScreenReplay` and `HandoffCheck` are marked Ignored as "dev-only CLI, path supplied by the local user, no trust boundary"; `WorktreeIncludeCopier` line 31 as a false positive citing the new tests; `FolderDeletion` closes with the fix. This is done by hand in Aikido after the pull request merges.

## Risks / Trade-offs

- [The workflow edits cannot be tested locally] → the pull request's own `check` run proves `check.yml`; `release.yml` is proved on the next version bump, and the change does not touch any step that needs git credentials.
- [A junction test depends on `cmd.exe` and NTFS] → the CI runner is Windows on NTFS, and the integration tests already shell out to `cmd.exe`.
- [Removing a link instead of its contents changes what "delete the folder" means for a junction] → that is the intended behaviour: the target was never part of the leftover worktree.
- [The test fixture's own `Dispose` uses the old walk-through-links deletion] → the junction tests remove their junctions explicitly in `Dispose` before the shared clean-up runs.

## Migration Plan

One pull request on `aikido-vulnerabilities`, archived in the same pull request. No state or command-line change; released as a patch version when next bumped.
