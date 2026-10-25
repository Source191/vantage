# Proposal

## Why

The code rule says user-facing text lives only in `Wording`, and `session-name-validation` finished the launch path. Three places still compose sentences elsewhere. `RemovalDecision` builds six reasons in the Domain, such as "the worktree is locked" and "3 changed or untracked files would be lost", and they reach the footer and two outcomes as strings. `WorktreeHealth` builds five in the Domain, such as "registered with git but the folder is gone". `TypedConfirmationDialog` builds four in Tui: two about the typed text and two about the clipboard. A sentence built in the Domain cannot be changed in one place, and a reflection fact cannot see that a case has no words.

## What Changes

- **Removal reasons become facts.** `RemovalDecision.Reason` becomes a `RemovalReason` with cases `None`, `LiveSession(name)`, `Locked`, `LeftoverFilesWouldBeLost(count, atLeast)`, `ChangesWouldBeLost(count)`, `UnpushedCommits(branch, count)` and `LeftoverFolder`. `Footer.NotRemoved`, `RemoveWorktreeOutcome.Refused` and `DeleteSessionOutcome.KeptWorktree` carry the fact, and `Wording` says it with today's words.
- **Health reasons become facts.** `WorktreeHealth.Reason` becomes a `HealthReason` with cases `None`, `FolderGone`, `Locked(lockReason)`, `RegistrationGone`, `UnderClaudeWorktrees`, `EmptyLeftover` and `NotRegistered`. Git's own lock reason stays a string inside `Locked`, because it is git's words about a failure. `Footer.Unhealthy` carries the fact.
- **The typed-confirmation dialog's sentences move to `Wording`:** the nothing-typed and not-the-same lines, and the two clipboard outcomes. The dialog keeps deciding the match, as the wizard's dialog keeps its layout, because the decision is one string comparison inside a retry loop and the reducer already receives the result as `ForceConfirmed`.
- **Reflection facts** in `WordingShould` fail when a `RemovalReason` or `HealthReason` case has no words, as they do for footers.
- **Version:** a patch bump; every sentence keeps its words.

Out of scope: captions and key hints that never change, which stay with the layout as the wizard's did; failure reports from git, the Claude CLI and the file system, which say what another tool refused; and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Every sentence is unchanged; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Domain:** `RemovalDecision.cs`, `WorktreeHealth.cs`.
- **Application:** `Views\Footer.cs`, `Views\WorktreesView.cs`, `Actions\RemoveWorktreeHandler.cs`, `Actions\DeleteSessionHandler.cs`, `Wording.cs`, `WordingOfOutcomes.cs`.
- **Tui:** `TypedConfirmationDialog.cs`.
- **Tests:** `RemovalDecisionShould`, `WorktreeHealthShould`, `WorktreesViewShould`, `WorktreeInspectorShould`, the two handler tests, `WordingShould`, `WordingOfOutcomesShould`.
- **Docs:** `Version`.
