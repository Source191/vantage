# Proposal

## Why

The code review of 0.20.0-preview.7 found seven places where deleting a session, removing a worktree, launching into a new worktree or closing an empty tab does its side effects in an order, or with a catch, that leaves the machine half-done when one step fails: a session is gone but its worktree untouched with a raw "Failed:" line, a detached worktree offers to force-delete a branch that was never its own, one unreadable leftover subfolder blanks the whole worktrees scan, a worktree is created but never recorded, a locked leftover folder gives an exception message where the git path gives words, a process that has already gone fails adopt before the resume, and close-empty reports a tab closed that Windows Terminal kept open. All are small and sit on the paths that delete things, so they go first.

## What Changes

- **Deleting a session with a gone repository or a failing git (A-01):** `d` on a session in a Vantage-created worktree first checks the owning repository folder still exists, and treats a git failure while inspecting the worktree as a reason to keep it, so the session is deleted, the worktree is kept and the status line says why, never "Failed: …" with the delete half done.
- **Detached and unnamed heads (A-02, owner's decision):** removing a worktree whose head is detached, or whose head git could not name, removes it and asks about no branch; the folder name no longer stands in for a branch, so Vantage never offers `git branch -D <folder>` on a branch it has not shown to be the worktree's.
- **Leftover counting never fails the scan (I-09):** the leftover-file count skips a subfolder the user cannot read and never enters a junction or symbolic link, so one such folder does not fail the whole worktrees scan or the removal keys.
- **A new worktree is recorded before its includes are copied (A-17):** the launch records the worktree as Vantage's own and then copies the include files; a file-system failure in either is reported as "Could not start: …" with the worktree kept and recorded, so a later `d` treats its branch as Vantage's.
- **A leftover folder that will not delete reads as in use (A-18):** deleting a leftover folder that a tab or program holds open gives the same "still in use by a tab or program" sentence the git path already gives, not an exception message.
- **Ending a process tree that is partly gone (C-08):** ending a session's process tree no longer fails when a descendant has already gone or cannot be ended, so adopt reaches its resume and close-empty finishes the other sessions.
- **A tab whose root process is claude itself (A-05):** close-empty ends such a claude with exit code 0, so Windows Terminal closes the tab and "Closed 1 tab" is true; adopt keeps ending it and honestly reports the tab as not closed. Both handlers find the tab's shell through one domain helper.

Out of scope: a timeout on the one process runner and the sessions listing's single-flight guard (C-01, C-02, I-01, their own change); a gone repository on the Worktrees tab's `d`, which the scan already marks and `RemoveWorktreeHandler` already guards with `OwnerExists`; retyping `WorktreeRemovalFacts.Branch`, which is the Domain reviewer's area; `IDesktop` and `OpenInEditorHandler`, which the open change `editor-not-found` touches (the two changes are independent and can merge in either order); and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-attach`: delete keeps the worktree, and says why, when its repository is gone or git cannot inspect it; close-empty closes a tab whose root process is claude by ending it cleanly.
- `worktree-management`: a detached or unnamed head is removed without a branch question; a leftover folder that cannot be deleted because it is in use is reported as in use; counting a leftover folder's files never enters a link or fails on an unreadable subfolder.
- `session-launch`: a new worktree is recorded before its include files are copied, and a failed copy keeps the worktree, recorded, and does not start the session.

## Impact

- `Application\Actions\DeleteSessionHandler` (repository check, `GitException` caught into `KeptWorktree`), `Domain\RemovalReason` (two new cases, `RepositoryGone` and `GitFailed`) with their words in `Application\Wording.Describe(RemovalReason)` and facts in `WordingShould`.
- `Application\Actions\WorktreeRemover` (`BranchToDealWith` removed; `DeleteFolderAsync` catches `IOException` and `UnauthorizedAccessException` into `RemoveWorktreeOutcome.InUse`), `Application\Views\WorktreeRow` (the `Facts` doc), `RemoveWorktreeHandlerShould` (the detached-head fact rewritten, an in-use leftover fact added), `TestDoubles\FakeFolders` (a folder that refuses deletion).
- `Adapters\Files\Folders.Files` (an `EnumerationOptions`) with a junction fact in `FoldersShould`.
- `Application\Launch\LaunchService` (record before copy, the two exceptions caught into `LaunchOutcome.Failed`), `Ports\IWorktreeIncludeCopier` and `Ports\ICreatedWorktrees` (documented exceptions), a `FakeWorktreeIncludeCopier` in `TestDoubles` and a failing switch on `InMemoryState`.
- `Adapters\Processes\ProcessRegistry.WithProcess` (catches `AggregateException`).
- `Domain\TerminalTab.ShellAbove` answering a new `Domain\TabShell` closed hierarchy, `ShellOf` and its `NullFreeModelsShould` entry removed; `Application\Actions\CloseEmptyTerminalsHandler` and `AdoptSessionHandler` (one root-claude fact each).
- README: the Worktrees tab's `d` row (in use covers a leftover folder) and the branch paragraph (a detached worktree asks about no branch). `Version` raised in the implementing pull request (patch: fixes, no new key).
