# Tasks

## 1. FolderPath

- [x] 1.1 `FolderPathShould.EqualIgnoringCaseAndTrailingSeparators`, `ConvertForwardSlashes`, `ExposeNameAndParent`, `KnowWhenItIsUnderAnother`, `RejectRelativePaths`; implement `FolderPath` in `Domain`
- [x] 1.2 Replace `RepositoryPath` with `FolderPath` everywhere and delete it; verify build and tests pass
- [x] 1.3 `WorktreeRow.Path` and `Repository`, `CreatedWorktree.Path` and `Repository`, `SiblingWorktree.Path`, `RepositoryChoice.Path`, `LaunchPlan.Directory`, `TabLaunch.StartingDirectory`, `Session.WorkingDirectory` become `FolderPath`; delete `StateStore.SamePath`, `RepositoryCatalogue.SamePath`, `WorktreeInventory.Normalise` and the inline comparisons; verify build and tests pass and `grep OrdinalIgnoreCase` finds no path comparison outside `FolderPath`
- [x] 1.4 `JsonFileStoreShould.ReadFilesWrittenByTheCurrentVersion`: fixture JSON captured before this change reads back; add the `FolderPath` converter

## 2. Identifiers

- [x] 2.1 `SessionIdShould.RejectBlank`, `JobIdShould.BeEightHexCharacters` and `DeriveFromASessionId`, `BranchNameShould.RejectBlankAndWhitespace`; implement the three records
- [x] 2.2 Replace the string fields on `Session`, `SessionRow`, `TabRegistration` (owner becomes `TabOwner`, with `OwnTabShould` and `SessionRowEnricherShould` covering the Vantage case), `CreatedWorktree`, `BranchToConfirm`, `GitWorktreeEntry.Branch` and the handler effects; add converters; verify build, tests and the fixture-file test pass

## 3. Session hierarchy

- [x] 3.1 `ClaudeAgentsListingShould.MapBackgroundRowsAndInteractiveRows` and `MapARowWithNeitherPidNorIdToAStoppedBackgroundSession`; implement `BackgroundSession` and `InteractiveSession` and the mapping
- [x] 3.2 `SessionsViewShould`: the interactive-row cases pass with type patterns; rewrite `SessionAction`, `Rename` and `Adopt` to match on the subtype and remove `SessionKind` and every `JobId!`; verify `grep "JobId!"` finds nothing
- [x] 3.3 Handlers (`Stop`, `Delete`, `Rename`, `Adopt`, `CloseEmptyTerminals`, `Resume`, `OpenTab`) take the subtype they act on; their tests construct the subtype; verify build and tests pass
- [x] 3.4 `StateTransitionNotifier`, `WorktreeInventory.LiveSessionIn` and the table definitions match on the subtype; verify `dotnet run --project tools\HeadlessTerminal -- check` passes

## 4. Documentation

- [x] 4.1 Update CLAUDE.md Code rules (paths are `FolderPath`, ids are typed, sessions are a hierarchy) and the CLI facts if 3.1 changed the understanding; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 14: task group 1 (`FolderPath` everywhere, six comparison helpers deleted). Commit the type first, the sweep second.
- PR 15: task group 2 (`SessionId`, `JobId`, `BranchName`, `TabOwner`).
- PR 16: task groups 3 and 4 (the `Session` hierarchy, `JobId!` gone). Archive the change in this PR.
- Sequential after `outcomes-and-wording` PR 13: every one of these edits the handler and reducer files that PRs 10 to 13 reshape.
- PR 14 to 16 notes: ports keep strings and callers pass `.Value`, so `IGit`, `IFolders`, `IClaudeCli` and the state ports did not change shape; input from outside (git output, the CLI listing, state files, configured roots) goes through each type's `TryParse`, and a listing row without a full `cwd`, a session id or a derivable job id is skipped. State files keep their plain strings through converters, pinned by fixture tests committed before each sweep. The session base keeps the property name `SessionId` rather than the design's `Id`, which would have doubled the diff for no gain. Effects carry the session subtype they act on (`StopSession(BackgroundSession)`, `AdoptSession(InteractiveSession)` and so on), so handlers never re-check the kind; that made three outcomes unreachable, and they were deleted with their words and facts: adopt's "has no process to move", close-empty's "has no process" skip and open-tab's "Only background sessions can be opened in a tab.".
