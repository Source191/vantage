# Proposal

## Why

The facts Vantage reads about tabs, worktree folders, processes, sessions and releases use null as a value: a `TabRegistration` is pending while its `ProcessId` and `ProcessStartedAt` are both null; a `WorktreeObservation`'s `LockReason` is null when unlocked and empty when locked without a reason, and its `GitFileTargetExists` (`bool?`) and `OwnerRepository` together encode four states of a `.git` file; a `ProcessTree` member's `Usage` is null when it could not be measured; `SessionRow.Memory` is null when there is nothing to measure; `Session.WaitingFor` is null when the listing says nothing; and `ReleaseVersion.Prerelease` is null for a release. The owner decided on 31 October 2026 that models and results are null-free, with null confined to the line that receives it from outside Vantage, using named cases or null objects per concept and never a generic `Option<T>`. This change was split from `null-free-rows` to keep each pull request reviewable.

## What Changes

- **`TabRegistration.Claim`**: a closed `TabClaim` (`Pending`, `Claimed(int ProcessId, DateTimeOffset StartedAt)`) in place of the two nullable fields. Tab files keep exactly today's JSON (`processId` and `processStartedAt` null while pending), through a `TabFile` record in `Adapters\State` that the store maps to and from, so files written by either version read in the other.
- **`WorktreeObservation`**: `WorktreeLock Lock` (`Unlocked`, `Locked(string Reason)`, the reason empty when git gives none) and `GitPointer Pointer` (`NoGitFile`, `TargetGone`, `TargetExists`, `RegisteredElsewhere(FolderPath Owner)`) in place of `LockReason`, `GitFileTargetExists` and `OwnerRepository`. `Ports\GitWorktreeEntry` carries the same `WorktreeLock` and an `IsPrunable` flag in place of `LockReason` and `PrunableReason` (the prunable text is read nowhere); `GitWorktreeListing` translates git's lines.
- **`ProcessTree.Usage`**: a `ProcessMeasure` (`Measured(ProcessUsage Usage)`, `Unmeasured`); `IProcessRegistry.UsageOf` keeps its `ProcessUsage?`, translated where the tree is built.
- **`SessionRow.Memory`**: a `SessionMemory` (`Measured(MemorySize Size)`, `NothingToMeasure`), never `MemorySize.Zero`, so the detail pane still reads "nothing to measure".
- **`Session.WaitingFor`**: a `WaitingReason` (`Nothing`, `Given(string Text)`), translated in `ClaudeAgentsListing`.
- **`ReleaseVersion.Prerelease`**: empty text for a release. Semantic versioning forbids an empty prerelease and `TryParse` already refuses one, so empty is unambiguous.
- Every displayed text, health, removal decision, tab liveness and update offer stays as today.
- **Version:** `0.19.0-preview.7`, one step above main when it merges.
- `skip_specs: true`: no behaviour changes for any file Vantage writes. The one difference is in a tab file edited by hand to hold a process id without its start time (or the reverse), which now reads as pending; no version has written one, hand-edited state files are not supported, and the owner accepted this on 31 October 2026 (see design.md, "Tab files go through a `TabFile` record").

Out of scope: the branch, tab and session facts on the rows, which are `null-free-rows`; `ProcessTree.Under`'s nullable answer, which is `null-free-outcomes`; `IProcessRegistry`'s nullable reads (`StartTimeOf`, `ParentOf`, `UsageOf`) and `TerminalTab`'s and `TabRegistration.OwningTerminal`'s `int?`, which are the operating system's shape and feed `IWindowsTerminal.FocusTabAsync(int?)` in `null-at-the-boundary`; `BackgroundSession.ProcessId`, which is `null-free-sessions`; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** new `Domain\TabClaim.cs`, `Domain\WorktreeLock.cs`, `Domain\GitPointer.cs`, `Domain\ProcessMeasure.cs`, `Domain\SessionMemory.cs`, `Domain\WaitingReason.cs`; `Domain\TabRegistration.cs`, `Domain\WorktreeObservation.cs`, `Domain\WorktreeHealth.cs`, `Domain\ProcessTree.cs`, `Domain\Session.cs`, `Domain\ReleaseVersion.cs`; `Ports\GitWorktreeEntry.cs`; `Infrastructure\State\JsonFileStore.cs` and a new `Infrastructure\State\TabFile.cs` (the `KeepTabOwnerAsSessionId` modifier goes, since the record keeps the file's name), `Infrastructure\Git\GitWorktreeListing.cs`, `Infrastructure\Claude\ClaudeAgentsListing.cs`; `Application\Actions\SessionTabs.cs`, `Application\Actions\OpenTabHandler.cs`, `Application\Actions\OwnTab.cs`, `Application\WorktreeInventory.cs`, `Application\WorktreeInspector.cs`, `Application\MemoryBreakdown.cs`, `Application\SessionRowEnricher.cs`, `Application\Views\SessionsView.cs` (`SessionRow.Memory`), `Application\WordingOfDetail.cs`.
- **Tests:** `TabRegistrationShould`, `JsonFileStoreShould`, `SessionTabsShould`, `OpenTabHandlerShould`, `OwnTabShould`, `WorktreeHealthShould`, `GitWorktreeListingShould`, `WorktreeInventoryShould`, `WorktreeInspectorShould`, `ProcessTreeShould`, `MemoryBreakdownShould`, `SessionRowEnricherShould`, `WordingOfDetailShould`, `ClaudeAgentsListingShould`, `ReleaseVersionShould`.
- **Docs:** `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 5 of the null programme, which merges one change after another in this order: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (this one, preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between. This change assumes `null-suppressions` (the `!` operators in `ReleaseVersion` and `WorktreeInventory.OwnerOf`, and `ReleaseVersion.TryParse`'s shape), `null-free-outcomes` (`ProcessTree.Under`'s answer) and `null-free-rows` (`GitWorktreeEntry.Head`, `SessionRow`'s shape) have merged.
