# Tasks

Each task is one red-green-refactor cycle (or smaller), with the test named before the code. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Torni.Infrastructure.Tests -c Debug --no-build`, or the Core suite where the task says so. Every `JsonFileStoreShould` fact writes its raw JSON into the class's temporary folder, never the real `%LOCALAPPDATA%\Torni`, and uses Northwind fixtures and `C:\Users\me` paths.

## 1. Facts to check first

- [ ] 1.1 For each member design.md's table makes required (`TabFile`'s `Id`, `SessionId`, `Colour` and `CreatedAt`; `CreatedWorktree`'s four; `RecentRepository`'s `Path` and `UsedAt`; `UpdateCheckFile`'s `CheckedAt`), read `git log -p` on the file that declares it and confirm every released version that wrote the file wrote that member. Record the answer in design.md's first risk; a member some version left out becomes optional with a default in group 3 instead. Verify by the recorded answer.

## 2. The records say what is optional (Ports)

- [ ] 2.1 Guard, not a red: `JsonFileStoreShould.ReadAnEditorNamingOnlyItsPreset` writes `{ "editor": { "preset": "Rider" } }` and asserts the editor reads as Rider and every other member as missing; it passes today and stands guard over 3.1, which would refuse it without this task. Then: every member of `Ports\UserConfig` and `Ports\EditorSetting` takes `= null`, `UserConfig.Missing` becomes `new()`, and the record's summary says every member is optional. Verify the Core and Infrastructure suites pass, `ReadAnEarlierConfigWithoutTheNewProperties`, `ReadAConfigWithoutABaseAsHavingNone` and `ReadAConfigWithoutAPlaceAsHavingNone` included, and that `NullFreeModelsShould` is unchanged.

## 3. The state files refuse what cannot be (Infrastructure)

- [ ] 3.1 Red: `JsonFileStoreShould.SkipACreatedWorktreeWithoutABranch` writes one full created-worktree record and one with no `branch`, and asserts only the full one is read; `SkipACreatedWorktreeWhoseBranchIsNull` the same with `"branch": null`. Both fail today (the record is read with a null `BranchName`). Green: `RespectNullableAnnotations = true` and `RespectRequiredConstructorParameters = true` in `JsonFileStore.Options`, with `= null` on `TabFile`'s `ProcessId` and `ProcessStartedAt`, `UpdateCheckFile`'s `Newest` and `LastRunFile`'s `Version`. Verify the Infrastructure suite passes, `ReadATabFileHoldingAProcessWithoutItsStartTimeAsPending`, `ReadAnUpdateCheckFileWithoutNotesAsHavingNone` and every earlier-file fact included.
- [ ] 3.2 Pin the other records: `SkipATabFileWithoutAColour`, `SkipATabFileWhoseOwnerIsNull` (the `sessionId` key read through `TabOwnerJsonConverter`), `SkipARecentRepositoryWhosePathIsNull` (read through `TextValueJsonConverter<FolderPath>`) and `ReadAnUpdateCheckWithoutCheckedAtAsNever`, each beside a full record or file that still reads. They pass once 3.1 is in, which is acceptable: say so in the commit. Any that fails shows a converter the nullability check does not reach; stop and record it in design.md's second risk before going on. Verify they pass.
- [ ] 3.3 Red: `ReadAConfigNamingASettingTwiceAsMissing` writes `repositoryRoots` twice and asserts `ReadConfigAsync` answers `UserConfig.Missing`; `SkipARecentRepositoryNamingItsPathTwice` writes a recent record naming `path` twice beside a full one and asserts only the full one is read. Both fail today (the last value wins). Green: `AllowDuplicateProperties = false`. Verify the Infrastructure suite passes.
- [ ] 3.4 Pin the leniency the settings spec now states: `ReadAConfigHoldingAnUnknownPropertyAsItsKnownSettings` (`repositoryRoots` and a `theme` key), `ReadAConfigWhoseNamesAreInAnotherCase` (`RepositoryRoots`), `ReadANullWorktreeRootAsMissing` (`"worktreeRoot": null` beside `repositoryRoots`) and `ReadACreatedWorktreeHoldingAnUnknownKey` (a full record with a `note` key). They pass at once and stand guard against a later move to `JsonSerializerDefaults.Strict`; say so in the commit. Verify they pass.
- [ ] 3.5 Refactor: the comment above `JsonFileStore.Options` says what each of the three options refuses, that unknown properties and other letter cases stay accepted, and why (design.md's first decision in one sentence); the class summary's "A missing, corrupt or unreadable file reads as empty" names a file that holds `null` for a value it must have, leaves one out or names a key twice. Verify the build and the Infrastructure suite.

## 4. Docs and version

- [ ] 4.1 `CLAUDE.md`'s null rule (the "Torni's own models and results never use null for meaning" line) says the state files' reader refuses a `null` or a missing value where the record has none and a key given twice, beside "null is met only on the line that receives it from outside". Verify by reading it against design.md, and that no em dash was introduced.
- [ ] 4.2 Raise `Version` in `src\Source191.Torni\Source191.Torni.csproj` one preview step above `main` when implemented (patch: a hand-damaged file is refused, nothing a user does every day changes), and one line under `## Unreleased`, `### Changed`, in `CHANGELOG.md`: `- A state file holding null where a value must be, leaving a value out or naming a key twice is now read as corrupt, as an unreadable file already is (#N)`, N being the pull request's number once it opens. Verify `.github\scripts\Assert-VersionBumped.ps1 -Base main -Head HEAD` and `.github\scripts\Assert-ChangelogUpdated.ps1 -Base main -Head HEAD` pass.

## 5. Whole-solution check

- [ ] 5.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate stricter-state-files --strict` all pass; record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it, syncing the two added requirements into `openspec\specs\settings\spec.md` and `openspec\specs\session-launch\spec.md`.
- Push and open the implementation pull request only when the owner asks.
