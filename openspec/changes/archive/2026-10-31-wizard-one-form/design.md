# Design

## Context

`WizardControls` places the filter and list at x 1 (44 wide) and the form at x 48 (44 wide), the preview at row 12 and the prompt at row 15 when the chooser shows, and at the left edge from row 6 for a fixed folder; `LaunchWizardDialog` sizes the full dialog at 90 % by 90 % and the fixed-folder one at 84 by 23. `WizardView` owns every rule (focus order, Enter, Start) and is untouched. The smoke script recognises the wizard by its tab title (`Vantage (New session)`) and the fixed-folder form by its dialog title.

## Goals / Non-Goals

**Goals:**
- One column at 80 cells, every control reachable at 120 x 36 with at least four repository rows.
- No change to the reducer, the keys or the fixed-folder form.

**Non-Goals:**
- Any new behaviour: Enter starting, Up and Down between fields, a command bar.

## Decisions

- **The column is 76 cells inside an 80-cell dialog**, tall as nine tenths of the screen. Rows from the top: the repository caption, the filter, the list (`Dim.Fill` minus the rows below, floor 4), then the name caption and field, the Where caption and selector (two rows), the base caption and selector (two rows), the preview, the prompt caption and a three-row prompt, the problem line and the hint, with the buttons in the dialog's own button row. At 36 rows the dialog has 32: the fixed rows take 20, so the list gets 8; at 40 rows it gets 12.
  - *Rejected: keeping the dialog 90 % wide.* A one-column form in 108 cells leaves the right half empty.
- **The fixed-folder layout keeps its own numbers.** Its rows already read as a column from the left edge; sharing the new constants would change its dialog height for no gain.
- **The hint stays** `Wording.WizardHint`'s current words; the layout gives them nothing new to say.
- **No Up and Down between fields.** The list and the two selectors own the arrows; a text field would need its own rule to hand them over, which is behaviour this change is not about.

## Risks / Trade-offs

- [A 36-row terminal shows eight repositories] → the filter narrows the list as it does today.
- [The smoke run's wizard step changes shape] → it keys on the tab title, not the layout.

## Migration Plan

One pull request; no state or settings change.
