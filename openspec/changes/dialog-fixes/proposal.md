# Proposal

## Why

Three dialogs failed their 0.15.0 checks. The solution chooser shows each solution as a button, so long names are cropped and sit side by side, and the tab read "Vantage" rather than "Vantage (Open solution)" while it was open. The settings dialog offers Visual Studio as the editor behind `e`, which the owner never wants there (`v` already opens solutions in Visual Studio), and calls the section "Editor" when the key opens a folder. In the new-session wizard, Tab from the repository list lands on Where while Enter lands on the name, Enter on the Where or base selector closes the dialog with nothing started, the "(no repository)" entries puzzled the owner, and the wizard opened from a worktree row (`s`) is mostly empty space and then needs a trip to the sessions view to open the session's tab.

## What Changes

- **A list for the solution chooser.** The dialog lists each solution as its path relative to the folder, one per line, with the arrow keys to choose, Enter to open and Esc to cancel, titled through the same helper as every other dialog.
- **Settings: open folder with.** The Visual Studio preset goes (a saved `VisualStudio` reads as VS Code, the other settings untouched); the section reads "Open folder with", the `e` key's hint reads "Open folder", and the worktree root's caption says the worktrees view lists every worktree wherever it is.
- **Wizard keys in form order.** The controls are ordered filter, list, name, Where, base, prompt, so Tab and Enter agree; Enter on a selector moves to the next field and never closes the wizard; only F5, F6 and the buttons start a session.
- **No "(no repository)" choices.** The roots are no longer offered as choices; `vantage here` already starts a session in any folder. (This is the author's reading of the owner's "why does it show at all"; keeping the entries at the bottom of the list is the alternative.)
- **A compact wizard for a fixed folder.** From a worktree row the dialog shows only the directory, the name, the preview and the prompt from its left edge, sized to them, and Enter in the name field starts the session and opens its tab (F5 still starts without one), so `s` is one key and one Enter.
- **Version:** a minor raise: a preset and a wizard choice are removed, and Enter changes meaning in the fixed-folder wizard.

Out of scope: the one-form wizard (Up and Down between fields, Enter offering start or start-and-open in the full wizard), the key bar, and multiple Claude accounts (a later change).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `folder-actions`: "The solution key opens the folder's solution" asks with a list and the dialog's tab title.
- `settings`: "The editor is a preset or a custom command" loses Visual Studio and names the section's words.
- `session-launch`: "Repository is chosen from configured roots" no longer offers a root; "A new worktree is the default launch mode" says what the fixed-folder wizard shows and does on Enter; an added "The wizard's keys move through the form in order".

## Impact

- **Core:** `Domain\EditorChoice.cs` (the preset enum), `Application\EditorCommand.cs`, `Application\Launch\RepositoryCatalogue.cs`, `Application\Views\WizardView.cs` (the field order and the fixed-folder Enter), `Application\Wording.cs` and `WordingOfScreens.cs` (the hint lines, the `e` hint, the preset words).
- **Infrastructure:** `Processes\EditorLocator.cs` (the Visual Studio lookup goes).
- **Tui:** `WizardControls.cs` and `LaunchWizardDialog.cs` (order, selectors' Enter, the compact layout), a new `ChooseSolutionDialog.cs` with `DialogEffects.cs` calling it, `SettingsControls.cs` (captions and the preset list).
- **Tests:** EditorChoiceShould, SettingsFileShould, EditorLocatorShould, EditorCommandShould, RepositoryCatalogueShould, WizardViewShould, WordingShould, WordingOfKeysAndColumnsShould, DialogEffectsShould; the smoke script gains the fixed-folder wizard and the solution chooser.
- **Docs:** README (the keys table, the wizard section, the worktrees view's `s`, the Settings section); the three specs.
- **Release:** a minor version raise; the hand-off check and the smoke check, plus a terminal check of the chooser's tab title.

## Order

Pull request 5 of six for the 0.15.0 test findings, which merge one after another in this order, each raising the version one preview step: 1 attach-and-command-line-clean-up, 2 header-tooltips-and-table-fit, 3 pull-request-lookups, 4 session-tabs-and-delete, 5 dialog-fixes, 6 dependabot-version-bump. The redesign changes follow (the WHERE column, the slim header with the key bar and the Sessions and Worktrees tabs, the one-form wizard, the detail pane), and 0.17.0 with the preview suffix dropped is the release to publish once the six have merged.
