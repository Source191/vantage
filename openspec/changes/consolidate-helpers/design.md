# Design

## Context

See proposal.md and `docs\architecture-review.md` section 3. These items were deliberately kept out of the earlier changes so that each of those stayed single-purpose.

## Goals / Non-Goals

**Goals:** one gatherer of worktree facts, one builder of tab scripts, CLI vocabulary confined to the Claude adapter, one documented failure style on `IGit`.

**Non-Goals:** changing which facts are gathered or when, changing the PowerShell that runs in tabs, changing any display label.

## Decisions

- **`WorktreeInspector.InspectAsync(FolderPath repository, FolderPath worktree, BranchName? branch, bool createdByVantage)`** runs the same git calls the three sites run today (`ListWorktrees` for the entry and lock, `CountChangedFiles`, `CountUnpushedCommits`, `LastCommitAt`, `DefaultRemoteBranch` then `IsMerged`) and returns `WorktreeRemovalFacts` plus `LastCommitAt`. `WorktreeInventory` calls it for registered rows; `RemoveWorktreeHandler` and `DeleteSessionHandler` call it before `RemovalDecision.For`. The leftover-file count from `fix-view-and-state-defects` stays with the handler because it is a file-system fact, not a git one. Alternative rejected: caching inside the inspector; the enricher's thirty-second cache pattern is not needed on the fifteen-second worktree refresh.
- **`TabScripts`** grows from `Application\PowerShellScript` (the `Quote` and `Invoke` helpers moved there in `untangle-layers`) and adds the three script shapes. `Attach` produces the claim-tab line, `Clear-Host` and the attach line exactly as `OpenTabHandler` does today; `Trust` produces the cyan `Write-Host` line and a bare claude launch; `Shell` produces `Clear-Host`. It returns a `TabLaunch`, so the colour choice for trust (`TabPalette.Default[2]`) and shell (`TabPalette.Default[^1]`) tabs lives here too. `TabScriptsShould` carries the existing `OpenTabHandlerShould` and `TrustRecoveryShould` script assertions verbatim. The rule in CLAUDE.md that user text is never interpolated into PowerShell source is now checkable in one test class.
- **`ClaudeStates.From(pid, status, waitingFor, state)`** in the adapter returns `DisplayState`; `DisplayState` keeps its static instances and the `Unknown` factory that takes a label. `DisplayStateShould`'s cases move to `ClaudeStatesShould` unchanged. The session-listing spec's six display states are unaffected; only where the words are parsed changes.
- **`LaunchWorkflow` removal.** The wizard dialog (until `wizard-as-reducer` replaces it) takes `RepositoryCatalogue`, `IRecentRepositories` and `IFolders`; `LaunchWorkflow.Plan` becomes `LaunchPlanBuilder.Build(request, folders)` called directly. `StartSessionHandler`, which took over the trust tab in `effect-handlers`, asks `TabScripts.Trust` for it, with the explanation sentence in `Wording`.
- **`IGit` convention**: methods named `Count*`, `List*`, `Add*`, `Remove*`, `Prune*`, `Delete*` throw `GitException` on a non-zero exit; methods returning a nullable (`LastCommitAt`, `DefaultRemoteBranch`, `CurrentBranch`, `RepositoryRoot`, `MainCheckoutOf`) return null. `IsMerged` returns false on failure and says so. Each method's XML comment states which. `GitCliShould` gains one failure case per method that lacks one.

## Risks / Trade-offs

- [The inspector makes `WorktreeInventory` run `DefaultRemoteBranch` and `IsMerged` per row, which it does not today] → the inspector takes a `bool includeMergeState` and the inventory passes false, so the scan's process count is unchanged.
- [Removing `LaunchWorkflow` before the wizard rewrite touches the largest untested file] → the edit is constructor arguments only; the hand-off check and a manual wizard run are the acceptance, and `wizard-as-reducer` follows immediately.
