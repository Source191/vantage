# Proposal

## Why

`LaunchWizardDialog.cs` is 282 lines of Terminal.Gui closures and local functions, the largest untested file in the repository, and it holds real rules: which field gets focus after a failure, when the preview sentence changes, that Enter never starts a session, how the fixed-directory variant hides controls. The two table views already show the pattern that brings such rules under test: a pure reducer over state and input, with the Terminal.Gui code reduced to binding.

## What Changes

- `WizardState` (repository filter, choices, chosen repository, name, description, mode, base, prompt, problem, focus, fixed directory) and `WizardInput` (text changed, selection moved, mode changed, confirm repository, next field, start, start and open in tab, cancel) records in `Application`, with `WizardView.Reduce` as a pure function returning the new state and an optional `WizardEffect` (`Launch(plan, openInTab)`, `Close`).
- `LaunchPreview` and `LaunchPlanBuilder` are called from the reducer; the `problem` text and the field to focus become state.
- `LaunchWizardDialog` binds Terminal.Gui controls to the state: each control change raises a `WizardInput`, each reduced state is pushed back to the controls (text, visibility, focus, preview, problem).
- The keyboard model (arrows in the filter drive the list, letters in the list go back to the filter, Enter confirms the repository, Enter in a field moves on, F5 and F6 start) is expressed as inputs so it is tested.

Out of scope: any change to the wizard's fields, keys or sentences; a general form framework; multiple Claude accounts. Depends on `consolidate-helpers` (the dialog's dependencies) and `outcomes-and-wording` (its sentences).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The wizard behaves as the session-launch spec describes; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: new `Application\Views\WizardView.cs`, `WizardState.cs`, `WizardInput.cs`; `LaunchPreview` and `LaunchPlanBuilder` unchanged.
- `Tui`: `LaunchWizardDialog` shrinks to layout and binding, roughly a third of its size.
- Tests: `WizardViewShould` is new and covers every rule the dialog holds today; the wizard's sentences join `WordingShould`.
