# Design

## Context

`DeleteSessionHandler.DeleteAsync` (`src\Source191.Vantage.Core\Application\Actions\DeleteSessionHandler.cs:17-23`) runs `claude rm` and closes the tab, then `RowAsync` (`:54-65`) asks `WorktreeInspector.HealthAsync`, which calls `git.ListWorktreesAsync` on a repository the row hard-codes as existing (`OwnerExists: true`); `IGit` documents that call as throwing `GitException`, and nothing between there and `TableBody` catches it, where `WorktreeInventory.RegisteredRowsAsync` and `InspectedAsync` (`WorktreeInventory.cs:106, 152`) catch the same exception. `WorktreeRemover.RemoveThroughGitAsync` (`WorktreeRemover.cs:39-45`) switches on `BranchToDealWith(row)` (`:48-54`), which substitutes the folder name for a `Detached` or `Unknown` head, and `RemoveWorktreeHandlerShould.KeepTheFolderNameAsTheBranchOfADetachedWorktree` (`:30-36`) pins that; `RemovalDecision.BranchActionFor` (`RemovalDecision.cs:37`) never reads the head. `DeleteFolderAsync` (`WorktreeRemover.cs:63-79`) calls `folders.DeleteTree` with no catch after the `InsideTheRoots` guard. `Folders.Files` (`src\Source191.Vantage.Infrastructure\Files\Folders.cs:14`) uses the `SearchOption` overload, `LeftoverFiles.Count` (`LeftoverFiles.cs:23-26`) is its only caller, and `FolderDeletionShould` already makes junctions with `mklink /J`. `LaunchService.CreateWorktreeAsync` (`LaunchService.cs:46-56`) adds the worktree, copies, then records, under a catch for `GitException` only (`:15-25`). `ProcessRegistry.Terminate` (`ProcessRegistry.cs:24-31`) calls `Process.Kill(entireProcessTree: true)` inside `WithProcess(int, Action<Process>)` (`:201-221`), which catches `ArgumentException`, `InvalidOperationException` and `Win32Exception`. `TerminalTab.ShellOf` (`Domain\TerminalTab.cs:14`) answers the process itself when its parent is WindowsTerminal (`:31-34`); `CloseEmptyTerminalsHandler` (`:36-45`) then kills it and calls `EndCleanly` on the dead id, while `AdoptSessionHandler` (`:31-32`) excludes the case with a comment saying ending claude closes the tab. `ShellOf`'s nullable is kept in `NullFreeModelsShould.cs:39`.

## Goals / Non-Goals

**Goals:**
- Every failure after a side effect on these paths ends in an outcome with words, never an exception reaching the status line, and the side effects already done are reported truthfully.
- No branch is ever acted on that git did not name as the worktree's.

**Non-Goals:**
- Changing `RemovalDecision`'s rules or `WorktreeRemovalFacts.Branch`'s type.
- Making the Worktrees tab's `d` handle a gone repository differently: the scan marks it and `RemoveWorktreeHandler` reads `OwnerExists`.
- Retrying a failed include copy or deleting the worktree it leaves.

## Decisions

### A gone repository and a git failure are two new removal reasons, not new outcomes
`DeleteSessionHandler.RemoveWorktreeAsync` reads `folders.DirectoryExists(created.Repository.Value)` before the inspector and answers `DeleteSessionOutcome.KeptWorktree(name, new RemovalReason.RepositoryGone(created.Repository))`; the two inspector calls and `RemovalDecision.For` sit in one `try` whose `catch (GitException failure)` answers `KeptWorktree(name, new RemovalReason.GitFailed(failure.Message))`. `Wording.Describe(RemovalReason)` gains "its repository C:\… no longer exists; remove it from the Worktrees tab" and "git could not inspect it: <reason>; check it on the Worktrees tab"; `KeptWorktree` is already Notice in `StatusTonesShould`, and `WordingShould.HaveWordsForEveryRemovalReason` finds the new cases by reflection. The git message is shown only to report what failed, which the wording rule allows. Alternative: reuse `RepositoryOutsideRoots`, whose words tell the user to add a root that no longer exists.

### A detached or unnamed head is removed and nothing more
`RemoveThroughGitAsync` switches on `(decision.Branch, row.Head)` and `BranchToDealWith` goes: `(_, Head.Detached or Head.Unknown)` is `Removed`, the two `OnBranch` arms stay, and the discard arm still throws `UnreachableException`. `WorktreeRow.Facts`'s doc (`Application\Views\WorktreeRow.cs:34`) is corrected to say the folder name is only the label a refusal's words use when no head is named, and that no branch is ever acted on by it; `Facts.Branch` itself stays a string. The pinned fact becomes `RemoveWorktreeHandlerShould.RemoveADetachedWorktreeWithoutAskingAboutAnyBranch`, with a sibling for `Head.Unknown`. Alternative: make `Ask` conditional on `OnBranch` in `RemovalDecision`, which belongs to the Domain reviewer and would duplicate the head check.

### The remover's folder deletion reports in use, after the roots guard
`DeleteFolderAsync` wraps `folders.DeleteTree(row.Path.Value)` in `catch (IOException or UnauthorizedAccessException)` answering `RemoveWorktreeOutcome.InUse(row.FolderName)`, so both views already have its words and tone. `FolderDeletion`'s own `IOException` for a root or relative path cannot reach this catch, because `InsideTheRoots` has already refused anything not strictly under a root; `FolderDeletionShould` keeps that refusal's fact at the adapter. `FakeFolders` gains `HeldOpen(path)`, a folder whose `DeleteTree` throws `IOException`. Alternative: a distinct `FolderInUse` outcome, two sentences for one cause.

### Files enumerates with options
`Folders` holds one static `EnumerationOptions` (`RecurseSubdirectories = true`, `IgnoreInaccessible = true`, `AttributesToSkip = FileAttributes.ReparsePoint`) for `Files`; `Directories`, `FilesIn` and `IsEmpty` keep their one-level calls. `FoldersShould.ListNoFileBehindAJunction` makes the junction with `mklink /J` as `FolderDeletionShould.CreateJunctionAsync` does. An unreadable subfolder is not made in a test (it needs an ACL change on the runner); `IgnoreInaccessible` is the runtime's documented behaviour. The `.git` pointer file is still filtered by `LeftoverFiles`.

### Record first, then copy, and two file-system exceptions become Failed
`CreateWorktreeAsync` calls `created.RecordCreatedWorktreeAsync` straight after `git.AddWorktreeAsync` and `copier.CopyAsync` last; `LaunchAsync`'s catch becomes `catch (Exception failure) when (failure is GitException or IOException or UnauthorizedAccessException)` answering `LaunchOutcome.Failed(failure.Message)`, whose words are already "Could not start: …". `IWorktreeIncludeCopier` and `ICreatedWorktrees` document that they throw `IOException` and `UnauthorizedAccessException` as file-system adapters. Tests use a `FakeWorktreeIncludeCopier` (`TestDoubles`, a boundary: the real `WorktreeIncludeCopier` copies files) that records calls and can throw, and `InMemoryState.FailNextCreatedWorktreeRecord(Exception)` (`InMemoryState` also implements `ILastRunVersion` since PR #180, which this does not touch). Alternative: catch in the adapters and answer a result, which would give every state write a result nobody reads.

### WithProcess also catches AggregateException
`WithProcess(int, Action<Process>)` gains `catch (AggregateException)`, documented as the shape `Process.Kill(entireProcessTree: true)` throws when a descendant has gone or cannot be ended: what could be ended is, and the caller carries on. The query overload is unchanged. No fact reproduces it, because a descendant that refuses termination cannot be made on a runner; `ProcessRegistryIntegrationShould.EndATreeWhoseChildHasAlreadyGoneWithoutThrowing` covers the nearest real case.

### One domain answer for the tab's shell
`TerminalTab.ShellAbove(processId, processes)` answers a closed `Domain\TabShell` hierarchy: `Above(int ProcessId)` for a shell between the process and WindowsTerminal, `TheProcess` when the process's own parent is WindowsTerminal, `None` when the walk ends without one; `ShellOf` goes with its `NullFreeModelsShould` entry, `TerminalOf` stays for `TabRegistration`. `CloseEmptyTerminalsHandler` switches: `Above` ends claude then the shell cleanly (Closed), `TheProcess` ends claude cleanly with `EndCleanly` and nothing else (Closed, because exit 0 closes the tab), `None` ends claude (Ended). `AdoptSessionHandler` ends claude with `Terminate` in every case and ends an `Above` shell cleanly after a continued resume, reporting `TabClosed` true only then; its comment says a root claude's tab stays open after a kill. Alternative: `ShellAbove` answering `int?` with null for both the root case and no tab, which close-empty cannot tell apart.

## Risks / Trade-offs

- [`EndCleanly` on a root claude is `TerminateProcess(handle, 0)`, which does not end claude's children] → a session with no messages has started no tools; the manual check in the completing pull request confirms the tab closes.
- [A detached worktree whose folder really is its branch's name loses the offer to delete that branch] → the owner's decision; the branch can be deleted with git.
- [`IgnoreInaccessible` also hides a count the user would want to see] → the row then under-counts, and `LeftoverFilesWouldBeLost` still refuses when anything readable remains.
- [Recording before copying leaves a record for a worktree whose includes are missing] → the worktree exists and is Vantage's, so `d` removes it; the status line names the failed copy.
- [`AggregateException` swallowed in `WithProcess` hides a real failure] → it is the documented shape of a partial kill; the tab's shell is still ended by the caller, and `ProcessIdentity` guards every id first.
