## Why

A session whose working directory has been deleted (a worktree folder removed by hand) makes `git` fail to start, with "An error occurred trying to start process 'git' with working directory", and the whole session listing fails at startup ("listing failed", zero rows). One missing folder should cost one row its repository and branch, not the entire screen.

## What Changes

- `GitCli` treats a working directory that does not exist as a failed git run, so its nullable queries answer null, `IsMergedAsync` answers false, and the throwing commands throw `GitException`, as the `IGit` contract already says.
- A session in a missing folder is listed with its folder name as the repository and a blank branch, as for a folder outside any repository.
- Patch version bump to 0.15.1.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-listing`: a session whose working directory no longer exists still appears in the list.

## Impact

- `src\Source191.Vantage.Infrastructure\Git\GitCli.cs` and its real-git tests in `GitCliIntegrationShould`.
- `Version` in `src\Source191.Vantage\Source191.Vantage.csproj`.
