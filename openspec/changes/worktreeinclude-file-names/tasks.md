# Tasks

## 1. Exact names from git's ignored-file listing

- [x] 1.1 Red: `WorktreeIncludeCopierShould.CopyFilesWhoseNamesHaveNonAsciiCharactersSpacesAndApostrophes` (real git): gitignored `Café.local.json`, `my app.local.json` and `it's.local.json` with `*.local.json` in `.worktreeinclude`; verify it fails because `Café.local.json` is neither reported nor copied, and commit
- [x] 1.2 Red: `GitCliShould.ListIgnoredFilesFromNulSeparatedOutput` replaces `ListIgnoredFilesAsRepositoryRelativePaths`: the arguments end in `-z`, and the answer `"src/appsettings.local.json\0secrets/key file.txt\0Café.local.json\0"` gives those three names unchanged, with no empty entry; verify it fails on the missing `-z`, and commit
- [x] 1.3 Green: `GitCli.ListIgnoredFilesAsync` passes `-z` and splits on NUL with empty entries removed and no trimming. `WorktreeIncludeCopierShould.CopierOverListing` joins its fake listing with NUL. Verify 1.1, 1.2, `SkipParentRelativePathsFromGit` and `SkipRootedAndNetworkPathsFromGit` all pass, and commit
- [ ] 1.4 Refactor: name the NUL splitting beside `Lines` in `GitCli` if it reads better, with a one-line comment on why `-z` (git quotes non-ASCII names otherwise); verify `dotnet build -c Debug`, `dotnet test -c Debug --no-build` and `dotnet format --verify-no-changes` pass, and commit

## 2. Release

- [ ] 2.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to a patch above `main`'s (the owner picks the next preview or the next release); verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` passes and `openspec validate worktreeinclude-file-names` is valid

## Workflow follow-up

- Archive the change in the pull request that completes it, so `openspec\specs\session-launch` gains the new scenario.
- After merging, mark the Aikido finding at `WorktreeIncludeCopier.cs:32` as not exploitable if Aikido is ever reinstalled, and point at this change.
