# Tasks

## 1. The rules as facts (Domain)

- [ ] 1.1 `RemovalDecisionShould`: each refusal asserts its `RemovalReason` case, a folder deletion asserts `LeftoverFolder` and a plain removal `None` (red first). Implement `RemovalReason` and change `RemovalDecision.Reason`'s type. Verify the Domain tests pass.
- [ ] 1.2 `WorktreeHealthShould`: each observation asserts its `HealthReason` case, with `Locked` carrying git's lock reason (red first). Implement `HealthReason` and change `WorktreeHealth.Reason`'s type. Verify the Domain tests pass.

## 2. The words (Application)

- [ ] 2.1 `WordingShould`: `HaveWordsForEveryRemovalReason`, `HaveWordsForEveryHealthReason` and one verbatim fact per case with today's words; the existing `NotRemoved`, `Unhealthy` and `KeptWorktree` sentence facts build their footers and outcomes from the facts and keep asserting the same sentences. Implement `Wording.Describe(RemovalReason)` and `Describe(HealthReason)` and change `Footer.NotRemoved`, `Footer.Unhealthy`, `RemoveWorktreeOutcome.Refused` and `DeleteSessionOutcome.KeptWorktree` to carry them. Verify `WordingShould` and `WordingOfOutcomesShould` pass.
- [ ] 2.2 `WorktreesViewShould`, `RemoveWorktreeHandlerShould`, `DeleteSessionHandlerShould`, `WorktreeInspectorShould`: footers and outcomes carry the facts; `WorktreesView` checks `HealthReason.None` rather than an empty string. Verify the Core suite passes.

## 3. The dialog (Tui)

- [ ] 3.1 `WordingShould`: `NothingTypedYet`, `NotTheSame`, `CopiedToClipboard` and `ClipboardUnavailable` with the dialog's exact words. Implement the members and make `TypedConfirmationDialog` call them. Verify the Core suite passes, `dotnet run --project tools\HeadlessTerminal -- check` passes, and `grep -n '\$"' src/Source191.Vantage.Tui/TypedConfirmationDialog.cs` finds nothing; the table titles and counts in `TableScreen` are layout text and stay.

## 4. Release

- [ ] 4.1 Raise `Version` by a patch. Verify `openspec validate facts-not-sentences`, `.github\scripts\Assert-VersionBumped.ps1` and `dotnet format --verify-no-changes` pass.

## Workflow follow-up

- One pull request, archived in it. Independent of the text-value changes; after `remove-session-descriptions` to avoid a merge of the wizard area for nothing.
- Manual check: `d` on a dirty worktree shows "Not removed: 3 changed or untracked files would be lost. D forces after a typed confirmation."; `D` with nothing typed shows the nothing-typed line inside the dialog; F2 shows the copied line.
