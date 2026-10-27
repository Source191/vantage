## Context

`SessionRowEnricher` asks git about each session's working directory. `ProcessRunner` sets that folder as the child's working directory, and `Process.Start` throws a `Win32Exception` when it does not exist. `IGit`'s nullable queries are documented to answer null, so the enricher has no catch; the exception escapes and fails the whole listing.

## Goals / Non-Goals

**Goals:**
- A missing folder behaves like any failed git run.
- Fix at the source, in the git adapter, for every `IGit` method.

**Non-Goals:**
- Marking such rows as "folder missing" (a possible later enhancement).
- Changing `ProcessRunner` for other callers.

## Decisions

- `GitCli` routes every call through one private method that answers a failed `ProcessResult` (exit code -1, the reason as standard error) when starting git throws a `Win32Exception` with native error 267 (`ERROR_DIRECTORY`, "The directory name is invalid"), which is what `CreateProcess` reports for a missing working directory; any other start failure (git not installed is error 2) still surfaces. Matching the error code rather than checking `Directory.Exists` keeps the fake-runner tests, which use made-up folders, unchanged, and never touches the disc on the happy path. Nullable queries then answer null, `RunOrThrowAsync` throws `GitException` with the reason, and `IsMergedAsync` answers false, all without new branches in callers. Alternative: catch in `SessionRowEnricher`; rejected because it would leave the contract broken for other callers (worktree scan, pull requests).
- The check races with deletion, but a race only repeats the same failure for one refresh; accepted.

## Risks / Trade-offs

- A UNC or offline drive path may make `Directory.Exists` slow; the folders are local worktrees, so accepted.
