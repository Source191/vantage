# Tasks

## 1. Guarded deletion of leftover folders

- [x] 1.1 Red: `FolderDeletionShould.RefuseADriveRoot` (the first drive letter from Z down to D that `DriveInfo.GetDrives()` does not list, as `X:\`), `RefuseAShareRoot` (`\\vantage-no-such-host\share\`) and `RefuseARelativePath` (a GUID-named folder) each expect an `IOException` whose message contains the path; verify all three fail on the missing exception, and commit
- [ ] 1.2 Green: the guard at the top of `FolderDeletion.DeleteTree` (design.md, Decisions); verify the three tests and the existing `FolderDeletionShould` facts pass, and commit
- [ ] 1.3 `FolderDeletionShould.DeleteAFolderWrittenWithATrailingSeparator` (the throwaway tree's path plus `\`); verify it passes, so the guard does not mistake an ordinary folder for a root, and commit
- [ ] 1.4 Refactor: `IFolders.DeleteTree`'s summary says it refuses a root or a relative path; verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and commit

## 2. Release

- [ ] 2.1 Raise `Version` to a patch above `main`'s once PR #82 (0.16.2-preview.3) has merged, merging `main` first; verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes and `openspec validate folder-deletion-guard` is valid

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\worktree-management` gains the requirement.
