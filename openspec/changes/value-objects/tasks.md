# Tasks

## 1. FolderPath

- [x] 1.1 `FolderPathShould.EqualIgnoringCaseAndTrailingSeparators`, `ConvertForwardSlashes`, `ExposeNameAndParent`, `KnowWhenItIsUnderAnother`, `RejectRelativePaths`; implement `FolderPath` in `Domain`
- [x] 1.2 Replace `RepositoryPath` with `FolderPath` everywhere and delete it; verify build and tests pass
- [ ] 1.3 `WorktreeRow.Path` and `Repository`, `CreatedWorktree.Path` and `Repository`, `SiblingWorktree.Path`, `RepositoryChoice.Path`, `LaunchPlan.Directory`, `TabLaunch.StartingDirectory`, `Session.WorkingDirectory` become `FolderPath`; delete `StateStore.SamePath`, `RepositoryCatalogue.SamePath`, `WorktreeInventory.Normalise` and the inline comparisons; verify build and tests pass and `grep OrdinalIgnoreCase` finds no path comparison outside `FolderPath`
- [ ] 1.4 `JsonFileStoreShould.ReadFilesWrittenByTheCurrentVersion`: fixture JSON captured before this change reads back; add the `FolderPath` converter

## 2. Identifiers

- [ ] 2.1 `SessionIdShould.RejectBlank`, `JobIdShould.BeEightHexCharacters` and `DeriveFromASessionId`, `BranchNameShould.RejectBlankAndWhitespace`; implement the three records
- [ ] 2.2 Replace the string fields on `Session`, `SessionRow`, `TabRegistration` (owner becomes `TabOwner`, with `OwnTabShould` and `SessionRowEnricherShould` covering the Vantage case), `CreatedWorktree`, `BranchToConfirm`, `GitWorktreeEntry.Branch` and the handler effects; add converters; verify build, tests and the fixture-file test pass

## 3. Session hierarchy

- [ ] 3.1 `ClaudeAgentsListingShould.MapBackgroundRowsAndInteractiveRows` and `MapARowWithNeitherPidNorIdToAStoppedBackgroundSession`; implement `BackgroundSession` and `InteractiveSession` and the mapping
- [ ] 3.2 `SessionsViewShould`: the interactive-row cases pass with type patterns; rewrite `SessionAction`, `Rename` and `Adopt` to match on the subtype and remove `SessionKind` and every `JobId!`; verify `grep "JobId!"` finds nothing
- [ ] 3.3 Handlers (`Stop`, `Delete`, `Rename`, `Adopt`, `CloseEmptyTerminals`, `Resume`, `OpenTab`) take the subtype they act on; their tests construct the subtype; verify build and tests pass
- [ ] 3.4 `StateTransitionNotifier`, `WorktreeInventory.LiveSessionIn` and the table definitions match on the subtype; verify `dotnet run --project tools\HeadlessTerminal -- check` passes

## 4. Documentation

- [ ] 4.1 Update CLAUDE.md Code rules (paths are `FolderPath`, ids are typed, sessions are a hierarchy) and the CLI facts if 3.1 changed the understanding; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 14: task group 1 (`FolderPath` everywhere, six comparison helpers deleted). Commit the type first, the sweep second.
- PR 15: task group 2 (`SessionId`, `JobId`, `BranchName`, `TabOwner`).
- PR 16: task groups 3 and 4 (the `Session` hierarchy, `JobId!` gone). Archive the change in this PR.
- Sequential after `outcomes-and-wording` PR 13: every one of these edits the handler and reducer files that PRs 10 to 13 reshape.
