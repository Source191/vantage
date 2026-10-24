# Design

## Context

See proposal.md for motivation. Each defect is local; the fixes below keep to the current single-project layout so this change can merge before the restructure. Anchors are to the code as reviewed on 24 October 2026.

## Goals / Non-Goals

**Goals:** remove the eight defects with a test for each; leave every footer sentence and column unchanged except where a scenario in the delta specs says otherwise.

**Non-Goals:** moving types between namespaces, new ports, the generic screen, value objects. Those belong to the later changes.

## Decisions

- **`RemovalDecision` gains `bool CanBeForced`** (`Domain\RemovalDecision.cs`), true for the dirty and unpushed refusals and for the leftover-with-files refusal, false for lock and live-session refusals. `WorktreesView.Remove` reads it; `IsFinal` (`Screens\WorktreesView.cs:202`) goes. Alternative rejected: an enum of refusal kinds, more than the view needs today.
- **`SessionRowEnricher.locations` becomes a `ConcurrentDictionary`** (`Screens\SessionRowEnricher.cs:16`). The cache is advisory, so a lost update between two overlapping refreshes only costs one extra git call. Alternative rejected: a `SemaphoreSlim` around the whole load, which would serialise the two screens' refreshes and make the UI feel slower.
- **`StopSession` carries `SessionRow`** instead of a job id (`Screens\SessionsView.cs:57`, `Screens\Tui\SessionsScreen.cs:116`), matching `DeleteSession`, `AdoptSession` and `OpenTabForSession`.
- **`SessionRow.HasTranscript` loses its default** (`Screens\SessionsView.cs:7`); every construction site names it.
- **`VantageConfig.DefaultBase` is `WorktreeBase`**, serialised with `JsonStringEnumConverter` so `config.json` keeps `"RemoteDefault"` and `"Head"`. `LaunchWizardDialog` and both screens take the enum.
- **`JumpToSession` carries a session id.** `WorktreeRow.SessionName` becomes `SessionId` plus `SessionName` (the table still shows the name); `WorktreeInventory.LiveSessionIn` returns both. The sessions reducer gains a `SelectSession(string SessionId)` input so `Program.cs:108` no longer edits state by hand.
- **Leftover file counting is split.** The scan asks "any file other than `.git`?" with `Directory.EnumerateFiles(...).Any(...)` and stores `HasLeftoverFiles`; `WorktreeActions.RemoveAsync` counts in full only when the decision needs the number for its reason. `WorktreeRemovalFacts.LeftoverFileCount` stays, fed by the action.
- **One record per file for recent repositories and descriptions.** `StateStore` writes `recent\<hash>.json` and `descriptions\<sessionId>.json`, mirroring `tabs\`. On first read it imports a legacy `recent.json` or `sessions.json` and deletes it. The recent list is ordered by a `UsedAt` timestamp in each record, keeping the ten most recent. The session-launch spec says five while `StateStore.RecentLimit` has always been ten; the code's ten is what users have, so the spec is corrected by a delta rather than the limit lowered. Alternative rejected: a file lock around read-modify-write, which still leaves a window across two processes and adds retry logic.

## Risks / Trade-offs

- [The legacy import runs once in whichever process reads first] → import is idempotent: records are keyed by path or session id, and the old file is deleted only after every record is written.
- [`ConcurrentDictionary` hides rather than removes the overlap] → acceptable: the only shared state is a cache; the view state itself is only mutated on the UI thread through `app.Invoke`.
- [Changing `WorktreeRow` shape touches `WorktreeInventoryShould` and `WorktreesViewShould` fixtures] → keep the positional order and add the id beside the name so fixture edits are mechanical.
