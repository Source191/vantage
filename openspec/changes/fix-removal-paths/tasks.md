# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, or the Infrastructure tests where the task says so.

## 1. Domain

- [ ] 1.1 `TerminalTabShould.AnswerTheShellAboveAProcessUnderOne`, `...AnswerTheProcessItselfWhenWindowsTerminalStartedItDirectly` and `...AnswerNoneWhenNoAncestorIsWindowsTerminal`: `Domain\TabShell` (`Above(int ProcessId)`, `TheProcess`, `None`, one file, sealed cases) and `TerminalTab.ShellAbove`; the existing `ShellOf` facts move to it. Red, then green; remove `ShellOf` and its line in `NullFreeModelsShould.Kept` (`KeepNoStaleEntry` fails otherwise). Verify the Core tests pass, `TerminalOf` untouched.
- [ ] 1.2 `WordingShould`: `Describe(new RemovalReason.RepositoryGone(FolderPath.From(@"C:\Users\me\source\repos\Northwind.Orders")))` reads "its repository C:\Users\me\source\repos\Northwind.Orders no longer exists; remove it from the Worktrees tab" and `Describe(new RemovalReason.GitFailed("fatal: not a git repository"))` reads "git could not inspect it: fatal: not a git repository; check it on the Worktrees tab". Red, then green: the two cases in `Domain\RemovalReason`, the two arms in `Wording.Describe(RemovalReason)`. Verify `HaveWordsForEveryRemovalReason` and `StatusTonesShould` pass unchanged.

## 2. Adapters

- [ ] 2.1 `FoldersShould.ListNoFileBehindAJunction` (a `node_modules\pkg` junction made with `mklink /J` as `FolderDeletionShould` does, pointing at a folder with files; `Files` lists only the files outside it) and `...ListFilesBeneathEveryReadableFolder` (the existing nested case kept). Red, then green: one static `EnumerationOptions` in `Folders` with `RecurseSubdirectories`, `IgnoreInaccessible` and `AttributesToSkip = ReparsePoint`. Verify in `test\Source191.Vantage.Infrastructure.Tests`, the junctions removed in `Dispose`.
- [ ] 2.2 `ProcessRegistryIntegrationShould.EndATreeWhoseChildHasAlreadyGoneWithoutThrowing`: a `cmd` sleeper whose child has exited is ended with `Terminate` and nothing throws. Green already or red; then `catch (AggregateException)` in `WithProcess(int, Action<Process>)` with a comment naming `Process.Kill(entireProcessTree: true)` as its source. Verify the Infrastructure tests pass and the query overload is unchanged.
- [ ] 2.3 `TestDoubles`: `FakeFolders.HeldOpen(path)` (its `DeleteTree` throws `IOException`), `FakeWorktreeIncludeCopier` (records `(repository, worktree)` calls, `Throwing(Exception)`), `InMemoryState.FailNextCreatedWorktreeRecord(Exception)`. No test framework in the project. Verify `SealedTypesShould` and `OneTypePerFileShould` pass.
- [ ] 2.4 Document on `Ports\IWorktreeIncludeCopier.CopyAsync` and `Ports\ICreatedWorktrees.RecordCreatedWorktreeAsync` that they throw `IOException` or `UnauthorizedAccessException` when the file system refuses, as `IGit` documents `GitException`. Verify `dotnet build -c Debug` (XML doc warnings are errors).

## 3. Removing worktrees

- [ ] 3.1 `RemoveWorktreeHandlerShould.RemoveADetachedWorktreeWithoutAskingAboutAnyBranch` (replacing `KeepTheFolderNameAsTheBranchOfADetachedWorktree`: `Row(WorktreeHealthKind.Ok, head: new Head.Detached())` answers `RemoveWorktreeOutcome.Removed("Northwind.Orders-fix-123")` and no `branch` call reaches git) and `...RemoveAWorktreeWhoseHeadGitDidNotName` for `Head.Unknown`. Red, then green: the switch over `(decision.Branch, row.Head)` and `BranchToDealWith` removed from `WorktreeRemover`; the `Facts` doc in `Application\Views\WorktreeRow.cs` corrected. Verify the Core tests pass.
- [ ] 3.2 `RemoveWorktreeHandlerShould.ReportALeftoverFolderHeldOpenAsInUse`: an empty broken folder marked `HeldOpen` answers `RemoveWorktreeOutcome.InUse("Northwind.Orders-stale")` and no `prune` reaches git; `...StillRefuseAFolderOutsideTheRoots` stays `Refused(OutsideTheRoots)` for a `HeldOpen` folder at `D:\Northwind.Archive`. Red, then green: `catch (IOException or UnauthorizedAccessException)` around `DeleteTree` in `WorktreeRemover.DeleteFolderAsync`. Verify the Core tests pass.

## 4. Deleting sessions

- [ ] 4.1 `DeleteSessionHandlerShould.KeepTheWorktreeWhenItsRepositoryHasGone`: the shared `folders` fixture (`:19`) declares only the worktree today, so it gains `.WithDirectory(Repository)` first, and the new fact builds its own `FakeFolders` with the worktree alone; the outcome is `KeptWorktree("fix-123", new RemovalReason.RepositoryGone(FolderPath.From(Repository)))`, `claude rm` ran, no git request was made, and the worktree folder still exists. Red, then green: `folders.DirectoryExists(created.Repository.Value)` read before the inspector in `DeleteSessionHandler.RemoveWorktreeAsync`. Verify the eight existing facts still pass beside the new one.
- [ ] 4.2 `DeleteSessionHandlerShould.KeepTheWorktreeWhenGitFailsToInspectIt`: `worktree list --porcelain` answers exit 128 with "fatal: not a git repository"; the outcome is `KeptWorktree("fix-123", new RemovalReason.GitFailed(...))` whose message contains "not a git repository", `claude rm` ran, no `remove` or `prune` reached git and the folder is kept. Red, then green: one `try` around the inspector calls and the decision, `catch (GitException failure)`. Verify the Core tests pass.
- [ ] 4.3 `WordingOfOutcomesShould`: `Describe(new DeleteSessionOutcome.KeptWorktree("fix-123", new RemovalReason.RepositoryGone(...)))` reads "Deleted fix-123; kept its worktree because its repository C:\Users\me\source\repos\Northwind.Orders no longer exists; remove it from the Worktrees tab." and the `GitFailed` sentence likewise; both Notice in `StatusTonesShould`. Red, then green if either arm needs a change. Verify the facts pass.

## 5. Launching into a new worktree

- [ ] 5.1 `LaunchServiceShould.RecordTheWorktreeBeforeCopyingIncludes`: with the fake copier, the record exists by the time `CopyAsync` is called (assert inside the fake's callback or by order). Red, then green: `RecordCreatedWorktreeAsync` moved before `CopyAsync` in `LaunchService.CreateWorktreeAsync`. Verify the Core tests pass, the existing `CreateTheWorktree...` fact included.
- [ ] 5.2 `LaunchServiceShould.ReportAFailedIncludeCopyAndKeepTheWorktreeRecorded` (the fake copier throws `IOException("The process cannot access the file")`; the outcome is `LaunchOutcome.Failed` whose message contains it, no `--bg` request was made, and the store holds the worktree) and `...ReportAFailedRecordAsAFailure` (`FailNextCreatedWorktreeRecord(new UnauthorizedAccessException(...))`). Red, then green: the exception filter in `LaunchAsync`, and `LaunchOutcome.Failed`'s doc names the include copy and the record beside git and the CLI. Verify the Core tests pass.

## 6. Closing empty tabs and adopting

- [ ] 6.1 `CloseEmptyTerminalsHandlerShould.CloseTheTabOfAClaudeThatWindowsTerminalStartedDirectlyByEndingItCleanly`: tree `WindowsTerminal (1) <- claude (4242)`; `EndedCleanly` is `[4242]`, `Terminated` empty, `Closed` is `["repos-f6"]`. Red, then green: the switch over `TerminalTab.ShellAbove` in `CloseEmptyTerminalsHandler`. Verify the existing four facts still pass.
- [ ] 6.2 `AdoptSessionHandlerShould.ReportTheTabKeptWhenClaudeWasTheTabsRootProcess`: the same tree; after a continued resume `Terminated` is `[4242]`, `EndedCleanly` empty and the outcome `Moved("repos-31", TabClosed: false)`. Red (it may already pass), then green: `ShellAbove` in `AdoptSessionHandler` and its comment corrected to say a killed root keeps its tab. Verify the Core tests pass.
- [ ] 6.3 README: the Worktrees tab's `d` row says a leftover folder a program holds open is reported as in use too, and the branch paragraph says a worktree on a detached head is removed without a branch question. Verify by reading against the spec deltas and that no em dash was introduced.

## 7. Version and whole-solution check

- [ ] 7.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above the one on `main` when implemented; patch, minor or major as CONTRIBUTING's Versions says (patch here: fixes and no new key). Verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes.
- [ ] 7.2 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate fix-removal-paths --strict` all pass; then `tools\install-local.ps1` and by hand, from a tab not inside a Claude Code session: `C` on a `claude` started as a Windows Terminal profile's command line closes its tab; `d` on a worktree row whose folder a PowerShell tab sits in reads as in use; `d` on a session whose repository folder was renamed deletes the session and keeps the worktree with the new sentence.

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\session-attach`, `openspec\specs\worktree-management` and `openspec\specs\session-launch` describe the new behaviour. The open change `editor-not-found` touches `IDesktop` and `OpenInEditorHandler`, which this change does not: the two are independent.
