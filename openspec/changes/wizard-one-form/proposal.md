# Proposal

## Why

The new-session wizard is two columns: the repository filter and list on the left, the form on the right, so the eye and the focus jump sideways after the repository is chosen and the dialog reads as two things. dialog-fixes (#105) already put the keys in form order, made Enter on a selector move on, dropped the "(no repository)" choices and gave the worktrees view's `s` a compact one-column form; the UI review's option 9 asked for the full wizard to read the same way, top to bottom. The owner confirmed on 30 October 2026 that Enter is to go on never starting a session in the full wizard, so this change is layout only.

## What Changes

- **One column, read top to bottom:** Repository (the filter field with the list under it, recent repositories first), Session name, Where, Worktree base, the preview line, Opening prompt, the problem line, the hint line, and the buttons Start (F5), Start and open in tab (F6) and Cancel (Esc). Tab and Enter move down the form as #105 left them; Enter in the prompt adds a line; only F5, F6 and the buttons start.
- **The dialog fits 120 x 36:** 80 columns wide, as tall as the screen allows (nine tenths), the repository list taking the rows the rest leaves, at least four.
- **The fixed-folder form is untouched:** `s` on a worktree row keeps its compact dialog and its Enter-starts behaviour.
- **Version:** one preview step above `main`'s.

Out of scope: Enter starting a session in the full wizard (decided against, 30 October 2026), Up and Down moving between fields, a command bar in place of the wizard (option 14 of the review), and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: an added "The wizard reads as one form".

## Impact

- **Tui:** `WizardControls` (the layout and the list's height) and `LaunchWizardDialog` (the dialog's size); nothing in `WizardView` or `WizardState`.
- **Application:** `Wording.WizardHint` only if its words change with the layout.
- **Tests:** none in Core (no rule changes); the headless capture at 120 x 36 and the smoke step that opens and cancels the wizard.
- **Docs:** README's wizard section (the paragraph on the two columns), the spec.
- **Release:** a preview raise; the hand-off check (a dialog changed) and the smoke check.

## Order

The third of four changes from the UI review, after where-column and tabs-and-key-bar and before detail-pane. Its added requirement sits beside dialog-fixes' "The wizard's keys move through the form in order", which it does not change; dialog-fixes archives before it.
