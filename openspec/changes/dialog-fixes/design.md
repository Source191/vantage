# Design

## Context

`WizardControls.All` is the Tab order Terminal.Gui follows: the chooser views (filter, list, Where, base) come first, then the name and the prompt, which is why Tab from the list lands on Where while `WizardView.Next` sends Enter from the list to the name. `LaunchWizardDialog` binds `Accepting` on the filter, the list and the name only; an Enter on the Where or base selector raises an accept nobody handles, which the dialog takes as its own and closes with nothing started (the likeliest reading of the owner's report; the code shows the gap, his report the symptom). The dialog is always 90 % of the screen with the form at column 48; a fixed folder hides the chooser and leaves the left half empty. `RepositoryCatalogue.Choices` appends each root as "(no repository)". The solution chooser is `Confirm.Query` with one button per solution (`DialogEffects.ChooseSolution`); `Confirm.Query` already applies `DialogTabTitle`, so why the tab stayed "Vantage" is not settled from the code (this is the only dialog opened from a background completion, inside `app.Invoke`, rather than from a key). `EditorPreset` holds `VisualStudio`, which `EditorLocator` resolves to `devenv.exe`; `SettingsFile` reads the preset with `Enum.TryParse` and `Enum.IsDefined`, so a name the enum no longer has already reads as VS Code.

## Goals / Non-Goals

**Goals:**
- Tab and Enter agree everywhere in the wizard, and Enter never throws a half-filled form away.
- `s` from a worktree row is one key and one Enter to a session in its own tab.
- Every list the user chooses from shows whole names.

**Non-Goals:**
- Up and Down between fields and an Enter that offers start or start-and-open in the full wizard (the one-form wizard).
- Changing the `v` key's behaviour for one or a matching solution.

## Decisions

- **Form order in `All` and in the reducer.** `All` becomes filter, list, name, Where, base, prompt (captions in step), so Tab follows the form; `WizardView.Next` sends Enter from the name to Where when the chooser shows and to the prompt otherwise, from Where to the base, and from the base to the prompt. The selectors' `Accepting` raise `NextField`, and the dialog's own `Accepting` is marked handled so no stray Enter can close it; the buttons keep their handlers.
- **Enter in a fixed folder's name field launches with a tab.** In the reducer, `NextField` from the name when `FixedChoice` is set yields `Started(state, openInTab: true)`, so the rule is tested without Terminal.Gui; the hint line (`Wording.WizardHint`) reads "Enter starts and opens in tab   F5 start without a tab   Esc cancel" for a fixed folder. The prompt is still reachable with Tab before Enter. The full wizard keeps Enter as navigation because a stray Enter there could start a half-filled session in a new worktree.
  - *Rejected: a default button.* A button with IsDefault takes the initial focus (CLAUDE.md), which would pull the cursor off the name field the 4.3 test expects it in.
- **A compact fixed-folder dialog.** `WizardControls` takes the form column from the state: 48 with the chooser, 1 without, and `LaunchWizardDialog` sizes the dialog to the form for a fixed folder (width 84, height 20: directory, name, a two-line preview, the prompt caption and five lines, the problem line, the hint, the buttons) and keeps 90 % otherwise. The caption "Directory" and the preview already come from `Wording`.
- **Roots leave the catalogue.** `RepositoryCatalogue.Choices` no longer appends them; `RepositoryChoice.IsRepository` stays, because `vantage here` still describes a folder that is not a repository. The session-launch scenario that leaned on the choice goes with it.
- **`ChooseSolutionDialog`.** A `Dialog` with a `ListView` of each solution's path relative to the row's folder (`Path.GetRelativePath`), `DialogTabTitle.Apply(app, dialog, DialogName.OpenSolution)`, Enter (the list's `Accepting`) answers the index, Esc answers null, and a Cancel button for the mouse; `DialogEffects.ChooseSolution` calls it in place of `Confirm.Query`. The ListView's selected row uses the Focus role as the wizard's list does. The tab title is checked at the terminal once the dialog is rebuilt; if it still reads "Vantage", the task records whether `IsModalChanged` fires inside `app.Invoke` and the fix goes in `DialogTabTitle` for every dialog.
- **The preset goes from the enum.** Removing `EditorPreset.VisualStudio` makes the build name `EditorLocator`, `EditorCommand` and `Wording.Describe(EditorPreset)`; `SettingsFile`'s `IsDefined` guard turns an old `VisualStudio` into VS Code, and a `SettingsFileShould` fact pins that with the roots kept. The dialog's caption becomes "Open folder with (↑↓ then Space to choose)" and the `e` hint "Open folder" (ten characters, within the hint width); the worktree root caption adds that the list is not filtered by it.

## Risks / Trade-offs

- [A user who chose Visual Studio for `e` silently gets VS Code] → the README's Settings section says so, and `v` still opens solutions in Visual Studio.
- [Enter in the fixed-folder name field starts before the user typed a prompt] → the hint line says Enter starts; Tab reaches the prompt first; the prompt is optional.
- [The chooser's tab title problem lies outside the dialog] → the terminal check in the tasks decides, and the fix would be in the shared helper, one place.
