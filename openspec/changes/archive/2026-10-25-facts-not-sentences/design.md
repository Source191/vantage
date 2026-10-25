# Design

## Context

See proposal.md, Why. `RemovalDecision.For(facts, forced)` returns a record of outcome, flags and a `string Reason` that is empty for a plain removal, "leftover folder" for a folder deletion and one of five refusals otherwise; `WorktreesView` turns a refusal into `Footer.NotRemoved(Reason, OfferForce)`, `RemoveWorktreeHandler` into `RemoveWorktreeOutcome.Refused(Reason)` and `DeleteSessionHandler` into `DeleteSessionOutcome.KeptWorktree(Name, Reason)`, and `Wording` and `WordingOfOutcomes` wrap the string in "Not removed: ..." and "kept its worktree because ...". `WorktreeHealth.From(observation)` returns a kind and a `string Reason`, which `WorktreesView` shows as `Footer.Unhealthy` when it is not empty. `TypedConfirmationDialog` is a Terminal.Gui dialog with its own retry loop and no reducer. `LaunchProblem` is the pattern already landed: a record hierarchy described by `Wording`, with a reflection fact.

## Goals / Non-Goals

**Goals:**
- No sentence composed outside `Wording`, apart from layout text and other tools' words.
- The removal and health reasons typed, so a new case without words fails a test.

**Non-Goals:**
- Rewording anything.
- A reducer for the typed-confirmation dialog.
- Restructuring `RemovalDecision` itself; only its reason changes type.

## Decisions

- **Type the reason, keep the decision record's shape.** `RemovalDecision` keeps `Outcome`, `Forced`, `Branch`, `PruneRegistration` and `CanBeForced`; only `Reason` changes from `string` to `RemovalReason`. Alternative rejected: making `RemovalDecision` a hierarchy of `Refused`, `RemoveWorktree` and `DeleteFolder`, which is a better model but changes every consumer's pattern match for no user-visible gain; it can follow if the record keeps growing.
- **`RemovalReason` is an open hierarchy with a `None` case,** like `Footer`, described by `Wording.Describe(RemovalReason)`; the "Not removed: {reason}." and "kept its worktree because {reason}" wrappers stay where they are and call it. `LeftoverFilesWouldBeLost(int Count, bool AtLeast)` carries the "at least" flag that today's string builds from the file limit.
- **`HealthReason` likewise,** with `Locked(string LockReason)` keeping git's words: a lock reason is what git said, a failure report rather than a rule, so it stays a string inside the fact. `WorktreesView` checks `Reason is not HealthReason.None` instead of the string's length.
- **The dialog keeps its decision and loses its words.** `Wording.NothingTypedYet(kind, expected)`, `Wording.NotTheSame(typed)`, `Wording.CopiedToClipboard(expected)` and `Wording.ClipboardUnavailable` are plain members with verbatim facts, as the wizard's title and hint are. Alternative rejected: a reducer for the dialog; it has one rule and the reducer already gets `ForceConfirmed` when it closes.
- **Captions stay as layout text.** "New name, then Enter:" and the like never change and name no fact about the user's data, the same line `wizard-as-reducer` drew for `WizardControls`.
- **Reflection facts** for both hierarchies follow `WordingShould.HaveWordsForEveryFooterCase`; `Placeholder` learns the two types.

## Risks / Trade-offs

- [The wrapped sentences read differently once the reason is a fact] → every existing `WordingShould` and `WordingOfOutcomesShould` fact asserts the whole sentence and stays as the regression guard.
- [`RemovalDecisionShould` compares `Reason` strings in twenty-two places] → they become case comparisons, which is the point: a test that asserts `Locked` rather than "the worktree is locked" survives a rewording.
