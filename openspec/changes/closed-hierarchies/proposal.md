# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026; D-14, D-15, D-16 and A-20) found four places where the house pattern of a closed hierarchy, an abstract record with its sealed cases nested inside it, is not followed: `RemovalDecision` and `WorktreeObservation` are enum-plus-flags records whose factories fill the members that do not apply with placeholders (`BranchAction.None`, `RemovalReason.None`, a `GitPointer` for a registered worktree) and whose consumers switch on one member and read others; `Session` and `RemoteRepository` keep their cases in top-level files, the only two of the 43 abstract records in `Domain` and `Ports` that do; `GitPointer.TargetExists` is pinned by a Domain fact for an observation the inventory never builds; and `DeleteSessionOutcome` alone carries its `TabClosed` as an init property on the base, which `WordingOfOutcomesShould`'s constructor-driven theory cannot vary. The code is correct today; this change makes its shape say what its factories already guarantee, before the cleanup programme reaches the removal paths again.

## What Changes

- **`RemovalDecision` as a closed hierarchy:** `Refused(RemovalReason Reason, bool CanBeForced)`, `RemoveWorktree(bool Forced, BranchAction Branch)` and `DeleteFolder(bool Forced, bool Prune)`, built by the same `For(WorktreeRemovalFacts, bool)` rules. `RemovalOutcome` is deleted; `WorktreeRemover` switches on the case, and `WorktreesView.Remove` reads the reason and `CanBeForced` from `Refused` alone.
- **Placeholders dropped:** `BranchAction.None`, `RemovalReason.None` and `RemovalReason.LeftoverFolder` exist only to fill members that no longer exist (`src\Source191.Vantage.Core\Domain\RemovalDecision.cs:29,33,35` are their only producers), so they go, with their two `Wording` arms and two `WordingShould` facts.
- **`WorktreeObservation` as a closed hierarchy:** `Registered(bool FolderExists, WorktreeLock Lock, bool Prunable)` for a folder in git's listing and `Unregistered(GitPointer GitFile, bool UnderClaudeWorktrees, bool EmptyLeftover)` for one it omits. `WorktreeHealth.From` switches on the case first, so the registered arms no longer have to precede the unregistered ones; `WorktreeInventory` and `WorktreeInspector` build the case they observed.
- **`GitPointer.TargetExists`'s fact moved:** `WorktreeHealthShould` pins it under `.claude\worktrees`, the one place the inventory builds it, as Orphan with `UnderClaudeWorktrees`; its summary says it never changes the health there. Nothing a person sees changes (see design.md for the alternative the review preferred and why it is not in this change).
- **`DeleteSessionOutcome.TabClosed` positional:** the base becomes `DeleteSessionOutcome(bool TabClosed)` and every case passes it through, as `AdoptSessionOutcome.Moved` and `StopSessionOutcome` carry theirs; `DeleteSessionHandler` threads it instead of a `with`.
- **`Session` and `RemoteRepository` cases nested:** `Session.Background`, `Session.Interactive`, `RemoteRepository.GitHub` and `RemoteRepository.AzureDevOps` inside their bases, the four top-level files deleted; `RemoteRepository`'s constructor becomes private and its two `GeneratedRegex` methods are renamed because they would share the cases' names.
- **A reflection fact:** every type deriving from an abstract record in `Domain` or `Ports` is nested inside it (`Architecture\ClosedHierarchiesShould`), so the convention in `CLAUDE.md` is held by a test, with no allow-list.
- **Docs:** `CLAUDE.md`'s session line names `Session.Background` and `Session.Interactive`; the closed-hierarchy rule gains the fact's name.
- **Version and changelog:** one preview step above `main` when implemented (0.20.0-preview.14 if nothing lands first), because files under `src\` change; a refactor, so patch-level under semantic versioning. One line under Unreleased in `CHANGELOG.md`, under Internal, which `Assert-ChangelogUpdated.ps1` requires of every pull request that changes the packaged tool (tasks.md drafts it).
- `skip_specs: true`: no key, screen, message, health, removal outcome or state file behaves differently.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor kept green by the Domain facts, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Domain:** `RemovalDecision.cs`, `WorktreeObservation.cs`, `WorktreeHealth.cs`, `GitPointer.cs`, `BranchAction.cs`, `RemovalReason.cs`, `Session.cs`, `RemoteRepository.cs`; deleted `RemovalOutcome.cs`, `BackgroundSession.cs`, `InteractiveSession.cs`, `GitHubRepository.cs`, `AzureDevOpsRepository.cs`.
- **Application:** `WorktreeInventory` (five observation sites), `WorktreeInspector.HealthAsync`, `Actions\WorktreeRemover`, `Actions\DeleteSessionOutcome`, `Actions\DeleteSessionHandler`, `Views\WorktreesView.Remove`, `Wording.cs:110,116`; the session rename touches 26 Application files, three in Domain (`Session.cs` and the two deleted), two in Tui and `Adapters\Claude\ClaudeAgentsListing`, each a name change the compiler drives.
- **Tests:** `RemovalDecisionShould` and `WorktreeHealthShould` rewritten to the cases; `WordingShould`, `WordingOfOutcomesShould` and `DeleteSessionHandlerShould` lose the dropped members and the init syntax; new `Architecture\ClosedHierarchiesShould`; 24 test files take the session rename; `WorktreeInventoryLeftoversShould`, `WorktreeInspectorShould`, the real-git `WorktreeInventoryShould`, `WorktreesViewShould`, `RemoveWorktreeHandlerShould` and `OneTypePerFileShould` stay as they are and must stay green.
- **Other open changes:** `fix-removal-paths` (A-01, A-02, A-18) edits `WorktreeRemover` and `DeleteSessionHandler` and lands first; this change rebases onto it. `domain-parsing-edges` edits `RemoteRepository.cs`'s GitHub pattern and adds a fact that every `TryParse` type hides its constructor (which the private constructor here satisfies); whichever lands second rebases.
- **Changes that landed since this was written** (`main` 8dbf4150, 0.20.0-preview.13): `show-release-notes` (PR #180) added `Ports\LastRun` and `Ports\PackageNotes` and reshaped `UpdateCheckRecord.Checked`, every case nested, so the new fact passes them as written; `editor-not-found` (PR #178) added `OpenInEditorOutcome` and `OpenSolutionOutcome` cases, nested, under `Application`'s `Outcome`, outside the fact's scope; `performance-diagnostics` (PR #179) added top-level `DiagnosticEvent` cases, which the fact excludes by design; `standardise-maybe-null-when` (PR #175) and `tidy-composition-root` (PR #177) touch nothing this change edits. The rename count is unchanged: 56 files, 160 occurrences.
- **Users:** none.

## Out of scope

- Making a folder under `.claude\worktrees` whose `.git` file names a live registration Elsewhere rather than Orphan: a behaviour change against `openspec\specs\worktree-management\spec.md:20` that needs its own change with a delta spec (design.md).
- A private constructor on `Session`: its base is positional, as `Ports\StartResult` is, and the new fact gives the guarantee for Vantage's assemblies.
- `ListedName` on `Session` (D-07), `ResumeResult` as a hierarchy (D-11) and the other review items: each is its own change.
- Multiple Claude accounts, which is a later change.
