# Design

## Context

See proposal.md. The dialog's rules today, read from `LaunchWizardDialog.cs`: the chosen repository is the list's selected item unless a fixed directory was given; the preview is recomputed on name, mode and selection changes; `ConfirmRepository` fails with a message and focuses the filter when nothing is chosen, otherwise focuses the name; `TryFinish` fails on no repository (focus filter), blank name (focus name), or a plan problem (focus name); Enter in name moves to description, in description to mode; F5 and F6 finish; the fixed-directory variant hides the chooser and worktree controls and shortens the hint.

## Goals / Non-Goals

**Goals:** every rule above has a reducer test; the dialog contains no decisions, only bindings; the wizard looks and behaves identically.

**Non-Goals:** reusing the reducer for other dialogs (`NameDialog`, `TypedConfirmationDialog` are small and stay as they are), replacing Terminal.Gui's own focus handling for Tab.

## Decisions

- **State is one record** with a `Field` enum (`RepositoryFilter`, `RepositoryList`, `Name`, `Description`, `Mode`, `Base`, `Prompt`) for `Focus`, and `IReadOnlyList<RepositoryChoice> Choices` recomputed by the reducer from the recent list, the catalogue and the filter text. The catalogue and recent list are inputs at construction (`WizardState.Open(recent, catalogue, defaultBase, fixedDirectory)`), read once as the dialog does today, so the reducer stays pure.
- **Inputs mirror control events**: `FilterChanged(text)`, `ListMoved(delta)` and `ListPaged(delta)`, `NameChanged(text)`, `DescriptionChanged(text)`, `ModeChanged(LaunchMode)`, `BaseChanged(WorktreeBase)`, `PromptChanged(text)`, `ConfirmRepository`, `NextField`, `Start(openInTab)`, `Cancel`. The reducer returns `WizardUpdate(WizardState State, WizardEffect? Effect)`; effects are `Launch(LaunchPlan, bool OpenInTab)` and `Close`, matching today's `WizardResult(Plan, OpenInTab)`.
- **The dialog is a binder.** It builds the controls once, subscribes each control event to `Apply(input)`, and after every reduce pushes state to controls: texts, `Visible` on the hidden set, `Focus` by `SetFocus` on the control for `state.Focus`, the preview and problem labels, and the list's selected index. It suppresses the control events it causes itself with a `binding` flag, the usual guard. The `Launch` effect closes the dialog and returns `WizardResult` as today.
- **Validation and preview** keep using `LaunchPlanBuilder.Build` (with `IFolders` from `untangle-layers`) and `LaunchPreview.Describe`; the reducer is where they are called, so their messages flow into `state.Problem` and `state.Preview`. The sentences move to `Wording` under `outcomes-and-wording`'s rule.
- **Focus for keyboard navigation**: arrow keys typed in the filter become `ListMoved` with focus switching to the list; a printable key typed in the list becomes `FilterChanged` with focus back to the filter; both are reducer cases, so the "one list, two ways in" behaviour is tested.

## Risks / Trade-offs

- [Terminal.Gui raises events during `SetFocus` and `Text` assignment that re-enter `Apply`] → the `binding` guard, and `WizardViewShould` proves the reducer is idempotent for a repeated input so an accidental double-apply is harmless.
- [The fixed-directory variant has different hidden controls and hint text] → it is `state.FixedDirectory is not null` in the reducer and one `Visible` loop in the binder; one test per variant for each rule that differs.
- [The dialog still cannot be unit tested] → accepted; the hand-off check and the README's wizard section are the acceptance, and the dialog is now a third of its size with no branches.
