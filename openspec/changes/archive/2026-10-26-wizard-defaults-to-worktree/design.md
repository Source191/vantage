# Design

## Context

`WizardState.Open` sets `Mode` to `LaunchMode.CurrentBranch` for every opening, and `WizardControls` builds the Where `OptionSelector` with a hard-coded `Value = 0`. The dialog never pushes `Mode` back to the selector, so today the two only agree because both say "Current branch". The fixed-folder wizard (`s` on the worktrees view) hides the selector and relies on `CurrentBranch` to start in the folder itself.

## Goals / Non-Goals

**Goals:**
- One place decides the opening mode (`WizardState.Open`), and the selector shows whatever it decided.

**Non-Goals:**
- Pushing `Mode` to the selector on every reduce: nothing but the selector itself changes the mode after opening.
- Any change to `LaunchPlanBuilder`, `LaunchPreview` or the refusal for a non-repository choice.

## Decisions

- **`Open` picks the mode from the fixed folder:** `NewWorktree` when there is no fixed folder, `CurrentBranch` when there is one. Alternative: a separate default for the fixed case passed in by the caller; rejected, since the fixed wizard has no Where choice, so its mode is a consequence of being fixed, not a choice.
- **The selector's initial value comes from the state**, in `WizardControls`' constructor, the way `BaseRef` already takes `state.Base`. Setting `Value` before the `ValueChanged` handler is attached raises nothing the reducer sees, so no echo guard is needed.
- **No automatic switch for non-repository choices:** the user chose to keep the default and let Start refuse with the existing words ("A worktree needs a repository; choose one or start on the current branch.").
- **Minor version (0.13.0):** the default behaviour changes but no key, state file or command line does, which the project's versioning rule places in minor.

## Risks / Trade-offs

- [A user used to accepting the defaults gets a worktree they did not expect] → the preview line says so before Start, the README and the pull request description say the default changed, and the worktree can be removed with `d` on the worktrees view while it is clean.
- [Picking "(no repository)" now needs one more step] → the preview explains it as soon as it is chosen and the refusal names the fix.
