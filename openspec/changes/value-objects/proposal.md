# Proposal

## Why

Paths, session ids, job ids and branch names travel as strings. Case-insensitive path comparison is re-implemented in six places (`StateStore.SamePath`, `RepositoryCatalogue.SamePath`, `WorktreeInventory.Normalise`, `DeleteSessionHandler.cs`, `RemoveWorktreeHandler.cs`, `WorktreesView.cs:46`), and the rule that a background session has a job id while an interactive one does not is enforced by five `JobId!` null-forgiving operators. The repository's own coding rules (wrap primitives, no getters for invariants pattern matching can carry) ask for the types this change adds.

## What Changes

- `FolderPath` in `Domain`: a record wrapping a full, trailing-separator-free path with ordinal-ignore-case equality, `Name`, `Parent`, `IsUnder(other)` and `Combine`. `RepositoryPath` becomes a thin alias or is folded into it. Every path field on `Session`, `SessionRow`, `WorktreeRow`, `CreatedWorktree`, `TabLaunch`, `RepositoryChoice`, `LaunchPlan` and the state records uses it.
- `SessionId`, `JobId` and `BranchName` records in `Domain`, each wrapping its string with the validation the code already does informally (a job id is eight hex characters; a branch name is non-blank).
- `Session` becomes a sealed hierarchy: `BackgroundSession(SessionId, JobId, ...)` and `InteractiveSession(SessionId, ProcessId, ...)` sharing `Name`, `WorkingDirectory`, `StartedAt`, `State`. `SessionKind` goes; the reducers and handlers pattern match on the type.
- The six comparison helpers are deleted.

Out of scope: wrapping every remaining string (descriptions, prompts, footer text are free text), changing JSON on disc (the records serialise to the same strings), multiple Claude accounts. Depends on `untangle-layers`; independent of the screen and handler changes but easiest after `effect-handlers`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Structure only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: new `Domain\FolderPath.cs`, `SessionId.cs`, `JobId.cs`, `BranchName.cs`; `Session.cs` becomes a hierarchy; most records in `Ports` and `Application` change a field type.
- `Infrastructure`: `ClaudeAgentsListing` builds the right `Session` subtype; `JsonFileStore` gains converters so files keep plain strings.
- Tests: `FolderPathShould`, `SessionIdShould`, `JobIdShould`, `BranchNameShould` are new; fixtures across `Core.Tests` construct the new types (a mechanical edit).
