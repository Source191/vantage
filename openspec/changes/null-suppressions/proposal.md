# Proposal

## Why

Seventeen null-forgiving operators (`!`) tell the compiler a value cannot be null without the code showing why, so a later edit can turn any of them into a `NullReferenceException` that no warning catches. Two Domain types also break the text-value convention in CLAUDE.md (no static member answers null, no `Parse`): `ReleaseVersion.TryParse` returns `ReleaseVersion?` and `RemoteRepository.Parse` returns `RemoteRepository?`; `Architecture\TextValuesShould` misses both because neither wraps a single string. The owner chose on 31 October 2026 to remove null from Vantage's own models and results, keeping it only on the line that receives it from outside; this change goes first because it is the smallest and every later change builds on the house `TryParse` shape.

## What Changes

- **The seventeen `!` go**, each replaced by a pattern that carries the fact the `!` asserted:
  - `ProcessRegistry.WithProcess<T>` returns `default!` three times: it takes the answer for a gone or refusing process as a parameter, and an `Action<Process>` overload serves `Terminate` and `EndCleanly`, which today return a dummy `null` through `WithProcess<object?>`.
  - `TableBody`'s `detail!` and `pane!`: the source and pane already checked in `AskForDetail` are passed on to the read and show steps.
  - `WorktreeInventory.OwnerOf`'s `gitDir!`, `EditorChoice.FromFile`'s `executable!` and `arguments!`, `ReleaseVersion.TryParse`'s three `numbers[i]!.Value`, `ExceptionSummary`'s two `GetMethod()!`, `VantageIcon.Fit`'s `support!`, the `Path.GetDirectoryName(...)!` in `WorktreeIncludeCopier` and `JsonFileStore`, and `ConsoleScreen.ClearLater`'s `timer!` (added by the status-line change, PR #129, after the survey, so seventeen in all).
- **`ReleaseVersion.TryParse(string?, out ReleaseVersion)`** with `[MaybeNullWhen(false)]`, the shape every text value has, in place of the null-returning one; its callers (`UpdateCheckRule`, `NewestRelease.Recorded` from shared-snapshot, `ClaudeCodeSupport`) and about twenty test lines follow, the tests through one helper.
- **`RemoteRepository.TryParse(string?, out RemoteRepository)`** in place of `Parse`; `PullRequestCatalogue` and `RemoteRepositoryShould` follow.
- **`Architecture\ParsingShould`:** every static `TryParse` declared in `Domain` returns `bool`, and no `Domain` type declares a static `Parse`, so neither breach can come back.
- **Version:** `0.19.0-preview.4`, one step above main when it merges.
- `skip_specs: true`: no behaviour changes; every answer, layout and message stays as it is.

Out of scope, each in its own change of the programme: `row.Branch!` in `WorktreeInventory` (null-free-rows, through `Head`); the nullable detail pane fields in `TableBody`, its `detailKey` and `DetailContent?`, and `IScreenDefinition.Detail` (null-free-screens, which models a tab with no pane: a `NoDetail` with no rows would still draw the pane's divider row on the Config tab, so it is not a drop-in); `VantageIcon.Fit`'s nullable `SixelSupportResult` and the nullable `IProcessRegistry` answers (null-at-the-boundary); `EditorChoice.Problem` and the other "problem or null" returns (null-free-outcomes); `ReleaseVersion.Prerelease` (null-free-rows); and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** `src\Source191.Vantage.Core\Domain\ReleaseVersion.cs`, `RemoteRepository.cs`, `EditorChoice.cs`, `UpdateCheckRule.cs`, `ClaudeCodeSupport.cs`, `NewestRelease.cs`; `Application\WorktreeInventory.cs`, `PullRequestCatalogue.cs`, `Diagnostics\ExceptionSummary.cs`; `src\Source191.Vantage.Infrastructure\Processes\ProcessRegistry.cs`, `Git\WorktreeIncludeCopier.cs`, `State\JsonFileStore.cs`; `src\Source191.Vantage.Tui\TableBody.cs`, `VantageIcon.cs`.
- **Tests:** `ReleaseVersionShould`, `RemoteRepositoryShould`, `UpdateCheckRuleShould`, `UpdateCheckShould`, `ScreenLoopShould`, `WordingShould`, `WordingOfScreensShould`, `StartUpdateHandlerShould`, `UpdateVantageShould` (through a `ReleaseVersions` test helper), `ClaudeContractShould` (one line); new `Architecture\ParsingShould`.
- **Docs:** `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 2 of the null programme, which merges one change after another in this order: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (this one, preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between.
