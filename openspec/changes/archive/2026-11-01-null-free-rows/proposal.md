# Proposal

## Why

The Sessions and Worktrees rows use null to stand for a state, and often hold one fact in two places. `BranchName?` means a detached head, a leftover folder git no longer lists, a session outside a repository, or a branch git named that is not a valid name, and every action that uses the branch (deleting it, looking up its pull request, reading its merge state) has to remember to ask first: `WorktreeInventory.cs:59` silences the compiler with `row.Branch!`. `SessionRow.HasLiveTab` and `TabColour?` are set together from one dictionary and matched together; `WorktreeRow.SessionId?` and `SessionName?` are always set and cleared together. The owner decided on 31 October 2026 that models and results are null-free, with null confined to the line that receives it from outside Vantage, using named cases or null objects per concept and never a generic `Option<T>`.

## What Changes

- **`Domain\Head`**, a closed record: `OnBranch(BranchName Branch)`, `Detached` (git's listing says `detached`) and `Unknown` (a leftover folder git no longer lists, a session outside a repository or whose branch git did not give, or branch text that is not a valid name). It replaces `BranchName?` on `Ports\GitWorktreeEntry`, `WorktreeRow` and `SessionRow`, and `PullRequestCatalogue.Find` takes it. `GitWorktreeListing` translates git's porcelain; the port's unused commit text, today also called `Head`, becomes `Commit`. `IGit.CurrentBranchAsync` keeps its `string?`, translated once in `SessionRowEnricher`.
- **`Domain\LiveTab`**: `None` or `Open(TabColour Colour)`, replacing `SessionRow.HasLiveTab` and `SessionRow.TabColour`; `OpenTabForSession` carries it in place of its `bool`.
- **`Domain\WorktreeSession`**: `None` or `Running(SessionId Id, string Name)`, replacing `WorktreeRow.SessionId?` and `SessionName?`, and `WorktreeRemovalFacts.LiveSessionName string?`.
- **`Domain\WhereLabel`** becomes two cases, `Outside(Place)` and `Inside(Repository, Place)`, so its `string? Repository` goes, and its `string? Branch` goes altogether: no cell has ever been given a branch, so the bracket text and `For`'s branch parameter go with it (owner's decision, 31 October 2026); every cell reads as today. `CLAUDE.md`'s description of `WhereLabel` follows.
- **`Domain\RowFilter.Matches`** and `ListSelection`'s filter fields take non-nullable text; a row without a branch or a path adds no field.
- Every displayed text stays as it is today: "none" for the detail pane's branch and tab, "-" in the resume checklist, no branch in the WHERE cells, the same filter matches, and the same removal and branch-deletion answers.
- **Version:** `0.19.0-preview.6`, one step above main when it merges.
- `skip_specs: true`: no behaviour changes.

Out of scope: `TabRegistration`, `WorktreeObservation` and the lock and prunable facts on `GitWorktreeEntry`, `ProcessTree`'s usage and `SessionRow.Memory`, `Session.WaitingFor` and `ReleaseVersion.Prerelease`, which are `null-free-observations`; `SessionRow.Checkout`, `SessionRowEnricher`'s `Location` and `BackgroundSession.ProcessId`, which are `null-free-sessions`; `PullRequestLookup.Missing`'s `PullRequestHost?`, which is `null-at-the-boundary`; `ConfigView.PathOf` and the view tuples, which are `null-free-outcomes`; `IGit`'s nullable queries, which stay; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** new `Domain\Head.cs`, `Domain\LiveTab.cs`, `Domain\WorktreeSession.cs`; `Domain\WhereLabel.cs`, `Domain\RowFilter.cs`, `Domain\WorktreeRemovalFacts.cs`, `Domain\RemovalDecision.cs`; `Ports\GitWorktreeEntry.cs`; `Infrastructure\Git\GitWorktreeListing.cs`; `Application\Views\SessionsView.cs` (`SessionRow`, `OpenTabForSession`, filter), `Application\Views\WorktreeRow.cs`, `Application\Views\WorktreesView.cs`, `Application\Views\ListSelection.cs`, `Application\Views\ConfigView.cs` (filter only), `Application\SessionRowEnricher.cs`, `Application\WorktreeInventory.cs`, `Application\PullRequestCatalogue.cs`, `Application\Detail\DetailLoader.cs`, `Application\WordingOfDetail.cs`, `Application\Actions\WorktreeRemover.cs`, `Application\Actions\OpenTabHandler.cs`, `Application\Actions\DeleteSessionHandler.cs`; `Tui\SessionsScreenDefinition.cs`, `Tui\WorktreesScreenDefinition.cs`, `Tui\ResumeChecklistDialog.cs`.
- **Tests:** new `HeadShould`; `WhereLabelShould`, `RowFilterShould`, `RemovalDecisionShould`, `GitWorktreeListingShould`, `PullRequestCatalogueShould`, `SessionRowEnricherShould`, `WorktreeInventoryShould`, `DetailLoaderShould`, `WordingOfDetailShould`, `SessionsViewShould`, `WorktreesViewShould`, `OpenTabHandlerShould`, `RemoveWorktreeHandlerShould`, `DeleteSessionHandlerShould`; the `SessionTestData` and `WorktreeTestData` builders take a `Head`, `LiveTab` and `WorktreeSession`.
- **Docs:** `CLAUDE.md` (the `WhereLabel` sentence); `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 4 of the null programme, which merges one change after another in this order: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (this one, preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between. This change assumes `null-suppressions` and `null-free-outcomes` have merged: it takes `row.Branch!`, which `null-suppressions` leaves, and works on `WorktreesView`'s jump in whatever shape `null-free-outcomes` gave its tuples.
