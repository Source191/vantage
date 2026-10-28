# Proposal

## Why

A new worktree is meant to get every gitignored file its repository's `.worktreeinclude` matches. Vantage reads git's ignored-file listing line by line, and git (with its default `core.quotePath`) writes a name containing non-ASCII characters as a C-quoted string: `Café.env` comes back as `"Caf\303\251.env"` (checked on 28 Oct 2026). That text never matches a pattern, so the file is silently left out of the worktree. With a catch-all pattern such as `*` it does match, and the copy then fails on a file that does not exist. An Aikido SAST finding at `WorktreeIncludeCopier.cs:32` led here; it reported path traversal. That part is not exploitable: the paths come from git's own listing of local ignored files inside the repository, and the existing characterisation tests prove such paths are dropped. The quoting bug is real, though.

## What Changes

- **Exact names from git:** the ignored-file listing asks git for NUL-separated output (`-z`), which git never quotes, and keeps each name exactly as git gives it, without trimming.
- **Regression tests:** a real-git test whose ignored files have a non-ASCII name, a space and an apostrophe, all of which must reach the new worktree. An adapter test pins the `-z` argument and the NUL splitting. The copier's existing containment tests take their fake listing in the NUL form, so they keep proving that parent-relative, rooted and network paths are skipped.
- **Version:** a patch bump, because the packaged tool changes.

Out of scope: other git listings Vantage reads (`status --porcelain` is only counted, so quoting cannot change its answer); changing `.worktreeinclude` pattern handling; multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: the "Gitignored local files are copied into a new worktree" requirement gains a scenario for file names with non-ASCII characters and spaces.

## Impact

- **Code:** `src\Source191.Vantage.Infrastructure\Git\GitCli.cs` (`ListIgnoredFilesAsync`).
- **Tests:** `test\Source191.Vantage.Infrastructure.Tests\GitCliShould.cs` and `WorktreeIncludeCopierShould.cs`.
- **Version:** `src\Source191.Vantage\Source191.Vantage.csproj`, a patch above `main`'s.
