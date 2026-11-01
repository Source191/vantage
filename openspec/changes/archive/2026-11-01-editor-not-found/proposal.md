# Proposal

## Why

The Open folder with setting is never checked against the machine, and the failures that can follow don't all read well. A Custom editor whose executable has gone shows .NET's own "Failed: An error occurred trying to start process …" instead of Vantage's "Could not find …". That line in turn tells the user to "press , to choose another editor in settings", but `,` has done nothing since the Config tab replaced the settings dialog. A Rider installed by JetBrains Toolbox 2.x is never found, because Toolbox 2.0 moved its installs to `%LOCALAPPDATA%\Programs` ([Toolbox App 2.0](https://blog.jetbrains.com/toolbox-app/2023/08/toolbox-app-2-0-overhauls-installations-and-updates/)). The dialog lists presets without saying which are installed, so the user finds out only on `f`. And `s` on a solution whose type has no application shows Windows' message after "Failed:".

## What Changes

- On `f`, a Custom editor whose executable no longer exists is "Could not find <name>" like a missing preset, rather than .NET's start failure.
- The Open folder with dialog refuses to save a Custom executable that does not exist, as the roots dialog refuses a missing root. A Custom editor in `config.json` whose executable has gone is still kept (it may be on a drive not yet attached); `f` reports it.
- The not-found line points to the Config tab (3, then Enter on Open folder with) instead of the `,` key.
- An editor that is found but will not start (access denied, a broken install) is reported in Vantage's words with Windows' reason, rather than as a raw "Failed:" line.
- Rider is also looked for in each folder directly under `%LOCALAPPDATA%\Programs` holding `bin\rider64.exe`, where Toolbox 2.x installs it, and the newest of every Rider found is opened as now.
- The Open folder with dialog marks each preset it cannot find, for example `Rider (not found)`, once it has looked; such a preset can still be saved, since it may be about to be installed.
- `s` on a solution whose type Windows has no application for opens nothing and says no application opens that type, rather than "Failed: <Windows' message>".
- F3 (Browse) in the Open folder with dialog, while Custom is chosen, opens a file picker whose choice replaces the executable field, as the roots and worktree root dialogs' Browse does for folders.
- The Rider scenarios that still say `e` say `f`.

Out of scope: marking the Config tab's Open folder with row when its editor is missing (it is read on every Config tab refresh, and Visual Studio's check starts `vswhere`); a Toolbox install location the user has moved in Toolbox 2.1's settings (Custom covers it); VS Code Insiders, Cursor or other editors as presets; checking for a missing editor when Vantage starts; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `folder-actions`: the editor key reports a missing Custom executable and an editor that will not start, and points to the Config tab; the solution key reports a solution type with no application.
- `settings`: a Custom executable must exist to be saved, and one that has gone from `config.json` is kept; Rider is found where Toolbox 2.x installs it (and its scenarios say `f`); the dialog marks presets not found; F3 browses for a Custom executable.

## Impact

- `Application\Actions\OpenInEditorHandler` (Custom existence check, the start failure) and `OpenInEditorOutcome` (a new `EditorNotStarted` case); `Application\Actions\OpenSolutionHandler` and its outcome (a no-application case).
- `Ports\IDesktop` and two exceptions beside it, as `GitException` is beside `IGit`; `Adapters\Processes\Desktop` and `OpenFileScript` (a distinct exit code when Windows has no application for the file).
- `Adapters\Processes\EditorLocator` and `EditorFolders` (the user's Programs folder).
- `Domain\Settings.Validate`, `EditorChoice`, `SettingsProblem` (a missing executable); `Application\Views\SettingsState`, `SettingsInput`, `SettingsView` (file existence, located presets, a chosen file); `Tui\SettingsDialog`, `SettingsControls` (preset labels, F3 for a file).
- `Application\WordingOfOutcomes`, `Wording`, with their facts in `WordingShould`, `WordingOfOutcomesShould` and `StatusTonesShould`.
- README's Config tab section (Browse for a Custom executable, presets marked not found); `Version` raised in the implementing pull request (minor: F3 is new in that dialog).
