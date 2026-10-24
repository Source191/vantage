# Proposal

## Why

Several small duplications and misplacements remain after the layering changes: git-backed worktree facts are gathered three ways (`DeleteSessionHandler.FactsAsync`, `RemoveWorktreeHandler.IsMergedAsync`, `WorktreeInventory.InspectedAsync`); PowerShell for tabs is built in three places with two executable paths plumbed through constructors; the Claude CLI's vocabulary (`busy`, `idle`, `blocked`, `done`) is parsed inside `Domain.DisplayState`; `LaunchWorkflow` is a facade over three wizard queries now that `effect-handlers` moved starting a session to `StartSessionHandler`; `IGit` mixes throw-on-failure and null-on-failure without saying which. Each is small; together they are the remaining places where the same change has to be made twice.

## What Changes

- `WorktreeInspector(IGit)` in `Application` returns `WorktreeRemovalFacts` (health, dirty count, unpushed count, merged flag, last commit) for a worktree path; the three gatherers call it.
- `TabScripts(claudeExecutable, vantageExecutable)` in `Application` builds every PowerShell script and `TabLaunch`: `Attach(session, registration)`, `Trust(directory)`, `Shell(directory)`; `OpenTabHandler`, `TrustRecovery` and `OpenShellHandler` take it, and the executable paths leave their constructors.
- The mapping from listing fields to `DisplayState` moves to `Adapters.Claude.ClaudeStates`; `SessionListingEntry` leaves `Domain`; `DisplayState` keeps only the kinds and labels.
- `LaunchWorkflow` is deleted; the wizard takes `RepositoryCatalogue`, `IRecentRepositories` and `IFolders` directly, and `TrustRecovery` becomes part of `TabScripts`.
- `IGit` is documented method by method (throws `GitException` or returns null) and the two styles are made consistent: query methods return null, command methods throw.

Out of scope: any observable change, the wizard's internals (next change), multiple Claude accounts. Depends on `effect-handlers` and `value-objects`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Structure only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: new `Application\WorktreeInspector.cs`, `Application\TabScripts.cs`; `Domain\DisplayState.cs` and `SessionListingEntry.cs` shrink or move; `Launch\LaunchWorkflow.cs` and `TrustRecovery.cs` removed.
- `Infrastructure`: `Adapters\Claude\ClaudeStates.cs` new; `ClaudeAgentsListing` calls it.
- Tests: `WorktreeInspectorShould`, `TabScriptsShould`, `ClaudeStatesShould` (the cases from `DisplayStateShould`); `LaunchWorkflowShould` already went in `effect-handlers` (all its cases were `StartedAsync` ones, now in `StartSessionHandlerShould`).
