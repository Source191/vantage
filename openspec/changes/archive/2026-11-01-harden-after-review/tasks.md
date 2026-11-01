# Tasks

## 1. Domain rules

- [x] 1.1 `FolderPathShould.RefuseTextHoldingANul`: `TryParse(@"C:\Users\me\a" + "\0" + "b")` answers false and does not throw. Then the check in `FolderPath.TryParse`. Verify with the Core suite.
- [x] 1.2 `IdentifiersShould`: `SessionId.TryParse` accepts `81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6` and refuses `--dangerously-skip-permissions`, `-x`, `..\..\Northwind`, `a/b`, `a b` and text with a non-ASCII letter; `SessionId.From` throws for each refused text. Then the shape rule in `SessionId`'s constructor and `TryParse`. Verify with the Core suite and `TextValuesShould`.
- [x] 1.3 `ProgramLabelShould.NeverLabelAProgramWithAnOptionsValue`: `uvx --from git+https://x-access-token:secret@github.com/northwind/mcp northwind-mcp` is `northwind-mcp`, `npx --registry https://me:secret@registry.northwind.example -y @northwind/mcp` is `@northwind/mcp`, `uvx --from=git+https://t:s@h/x northwind-mcp` is `northwind-mcp`, and a candidate holding `:` falls back to the runner's name; the existing facts still pass. Then the option list and the colon rule in `ProgramLabel`.
- [x] 1.4 `TerminalTabShould.StopAtAParentYoungerThanItsChild`: a chain claude, pwsh, WindowsTerminal answers the pwsh; the same chain where the pwsh started after claude answers no tab; a parent whose start time is unknown answers no tab. Then the start-time check in `TerminalTab.Walk` over `IProcessRegistry.StartTimeOf`; `TabRegistrationShould` still passes.
- [x] 1.5 `WorktreeHealthShould.BeBrokenWithItsGitFileGoneWhenAPrunableFolderExists` (registered, prunable, folder exists: broken, `HealthReason.GitFileGone`) and the registered-and-gone case still `FolderGone`. Then the observation member, the reason and the arm in `WorktreeHealth.From`.
- [x] 1.6 `RemovalDecisionShould`: a broken `GitFileGone` row with twelve leftover files is refused with `LeftoverFilesWouldBeLost(12)` and can be forced; with none it is `DeleteFolder` with prune. Then any arm `RemovalDecision.For` needs (expected none beyond the existing broken-with-files arm).
- [x] 1.7 `RemovalReason.OutsideTheRoots` and its place in every switch over removal reasons, built by the first test that needs it (2.1).

## 2. Wording

- [x] 2.1 `WordingShould`: the `GitFileGone` health reason ("registered with git but its .git file is gone"), the `OutsideTheRoots` removal reason naming the folder and saying it is outside the repository roots and the worktree root, and the session delete's kept-worktree words with the file count and force-remove on the Worktrees tab; `WordingOfOutcomesShould`: the refused-outside-the-roots outcome; `StatusTonesShould`: their tones. Then the words in `Wording` and `WordingOfOutcomes` and the tones in `Wording.ToneOf`. Verify with the three suites and the reflection facts.

## 3. Adapters

- [x] 3.1 `ProgramLocatorShould.SkipARelativePathEntry` (folders `.` and `bin` before a full path that holds `git.exe`: the full path's file is answered) and a new `PathFoldersShould` (an entry `.`, one `tools`, one empty and one `C:\Program Files\Git\cmd`: only the last is kept). Then the `Path.IsPathFullyQualified` filter in `PathFolders`, with a `From(string? path)` the test can call.
- [x] 3.2 `PullRequestHostsShould.FindAzOnlyInFullPathFolders` (a `.` entry before the folder holding az.cmd answers the full one). Then `AzureCliExecutable.Locate` over `PathFolders`; `Program.cs` hands it the filtered folders.
- [x] 3.3 `EditorLocatorShould.IgnoreACodeShimInARelativeFolder` (`code.cmd` under a relative PATH entry is not used). Verify it passes once 3.1 filters `EditorFolders.ForCurrentUser`'s folders; no other change expected.
- [x] 3.4 `WindowsTerminalArgumentsShould.NamePwshByItsFullPath` (the argument after `--startingDirectory` and its value is `C:\Program Files\PowerShell\7\pwsh.exe`, with `;` escaping unchanged) and `WindowsTerminalShould.ThrowNamingPwshWhenItIsNotOnThePath` (no process started). Then `ForTab` taking the path and `WindowsTerminal(processes, fragment, wt, pwsh)`; `Program.cs` passes `programs.Locate("pwsh")`.
- [x] 3.5 `ClaudeAgentsListingShould.SkipARowWhoseSessionIdIsNotAnId` (rows with `--dangerously-skip-permissions` and `..\..\Northwind` skipped, the others shown). Verify it passes over 1.2; no other change expected.
- [x] 3.6 `ClaudeTranscriptsShould.NeverLookOutsideTheProjectsFolder` cannot be written against a refused id once 1.2 holds; instead verify every caller of `ITranscripts` passes a parsed `SessionId` (read `ClaudeTranscripts` and its callers) and record the result in the pull request.

## 4. Worktree removal

- [x] 4.1 `WorktreeInventoryShould.CountAPrunableWorktreeWhoseFolderExistsAsALeftover` (real git: add a worktree, write two files, delete its `.git` file): the row is broken with `GitFileGone`, `FolderExists` true and a leftover count of three (the two files and the checked-out file). Then `RegisteredRowsAsync` reading the disc and counting.
- [x] 4.2 `WorktreeInventoryShould.ShowAWorktreeOfABareRepositoryAsElsewhere`, `ShowACheckoutWithASeparateGitDirectoryAsElsewhere` and `ReadARelativeGitdirFromTheGitFilesFolder` (real git, the last with `git worktree add --relative-paths` and the test's current directory elsewhere): each is elsewhere naming the repository. Then the structural owner and the relative resolution in `BrokenWorktree`, through `FolderPath.TryParse`.
- [x] 4.3 `WorktreeInspectorShould`: a listed prunable worktree whose folder exists answers broken `GitFileGone`; a worktree git does not list whose folder exists answers broken without `FolderGone`; a listed worktree whose folder is gone answers `FolderGone`. Then `HealthAsync` reading the folder.
- [x] 4.4 `RemoveWorktreeHandlerShould.ReadTheFolderWhenTheKeyIsPressed` (a row scanned empty whose folder now holds three files is refused with three files) and `RefuseAFolderOutsideTheRoots` (a broken row at `D:\Northwind.Archive` with roots under `C:\Users\me\source`: refused `OutsideTheRoots`, nothing deleted, no prune; also refused when forced). Then the re-read in `RemoveWorktreeHandler` and the root guard in `WorktreeRemover.DeleteFolderAsync`, the roots from `CurrentSettings`.
- [x] 4.5 `DeleteSessionHandlerShould.KeepAWorktreeWhoseGitFileIsGoneWhileItHoldsFiles` (session deleted, folder kept, the outcome carries the count) and `KeepAWorktreeGitListsUnderAnotherSpelling` (the listing names another path for it: folder and files kept); the existing "worktree without its .git" fact now has an empty folder and still deletes and prunes. Then `RowAsync` counting leftover files for any health other than ok.
- [x] 4.6 `WorktreesViewShould`: remove on a broken `GitFileGone` row with files explains and offers force-remove, and force-remove asks for the typed folder name. Verify it passes with the reducer unchanged.

## 5. Process control

- [x] 5.1 `CloseEmptyTerminalsHandlerShould.LeaveAnotherTabsShellWhenTheParentIdIsReused` and `AdoptSessionHandlerShould.LeaveAnotherTabsShellWhenTheParentIdIsReused` (the fake registry gives the session's process a parent that started after it, under WindowsTerminal: the session's process is ended, the parent is not). Verify they pass over 1.4.

## 6. CI and repository

- [ ] 6.1 `publish.yml`: after the download, `gh attestation verify` on the package with `--repo Source191/vantage --signer-workflow Source191/vantage/.github/workflows/release.yml --source-ref refs/heads/main`, failing the run otherwise. Verify by running the workflow by hand on the newest release tag up to the environment approval and reading the verify step's output, then rejecting the deployment. Workflow edit done, and its verify command passes locally on the v0.20.0-preview.1 package and fails for a wrong signer workflow; the verification run is owed (owed: see PR).
- [ ] 6.2 `release.yml`: a `build` job with `contents: read` (version check, build-test-pack, upload the nupkg as an artefact) and a `release` job (`needs: build`, `if` the version changed, `contents: write`, `id-token: write`, `attestations: write`) that downloads, attests and runs `gh release create`, with no checkout. Verify the release made by this change's implementing pull request has its attestation (`gh attestation verify` on its nupkg) and its prerelease flag. Workflow edit done; the verification run is owed (owed: see PR).
- [ ] 6.3 `claude-contract.yml`: the contract job with `contents: read` uploads its results; a `report` job (`needs: contract`, `if: failure()`, `issues: write`) raises or comments on the `claude-cli` issue from them, still through `--body-file`. Verify with a manual dispatch (`gh workflow run claude-contract.yml`) and the run's job permissions in its log. Workflow edit done; the verification run is owed (owed: see PR).
- [x] 6.4 `Contract\ClaudeContract`: every listed session id matches the shape rule (`SessionId.TryParse` answers true for each). Verify with the contract suite (`$env:VANTAGE_CLAUDE_CONTRACT = 1`) from a trusted repository.

## 7. Documentation and version

- [x] 7.1 README's "What Vantage trusts": leftovers are deleted only under the roots and only without files unless forced, a `.git` file with a live target is never a leftover, relative PATH entries are skipped, the tab's PowerShell by full path, session ids; and the worktrees section's health list gains the `.git` file gone reason. Verify by reading it against the specs.
- [x] 7.2 `CONTRIBUTING.md` and `.claude\rules\checks.md`: the two release jobs, the attestation check in `publish.yml`, immutable releases, and the contract workflow's report job; `release.yml`'s comment says `publish.yml` verifies. Verify by reading each against the workflows.
- [x] 7.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` by a minor step (taken: `0.20.0-preview.6`, the 0.20.0 line being the minor step over the released 0.19 line). Verify with `.github\scripts\Assert-VersionBumped.ps1` in CI.

## 8. Integration checks

- [x] 8.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate harden-after-review --strict` all pass.
- [ ] 8.2 The smoke check of the packed tool (`dotnet run --project tools\HeadlessTerminal -- smoke <exe> <worktree-folder>`) exits 0, and a shell tab opened with `o` on a worktree shows `(Get-Process -Id $PID).Path` as the full pwsh path. (owed: CI runs the smoke check; the shell-tab check is a manual one, see PR)

## Workflow follow-up

- After the merge: `gh api -X PUT repos/Source191/vantage/immutable-releases` and confirm it answers enabled.
- Archive the change in the pull request that completes it.
