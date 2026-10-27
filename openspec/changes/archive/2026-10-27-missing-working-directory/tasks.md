## 1. Test first

- [x] 1.1 Red: in `GitCliIntegrationShould`, add a fact that deletes a worktree folder and asserts `RepositoryRootAsync`, `MainCheckoutOfAsync`, `CurrentBranchAsync`, `OriginUrlAsync` and `DefaultRemoteBranchAsync` answer null, `LastCommitAtAsync` answers null and `IsMergedAsync` answers false; and a fact that `CountChangedFilesAsync` throws `GitException`. Verify it fails with the Win32Exception.

## 2. Fix

- [x] 2.1 Green: in `GitCli`, answer a failed result for a missing directory in one private method used by every call. Verify the Infrastructure tests pass.
- [x] 2.2 Refactor if needed; keep `dotnet format --verify-no-changes` clean.

## 3. Release

- [x] 3.1 Raise `Version` to 0.15.1 in the csproj.
- [x] 3.2 Run `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `openspec validate missing-working-directory`.
- [x] 3.3 Archive the change in the same pull request.
