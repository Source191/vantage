# Proposal

## Why

Vantage's only settings live in `%LOCALAPPDATA%\Vantage\config.json`, which the user has to find and edit by hand, and the one placement rule that matters most day to day is fixed: every new worktree lands beside its repository, so `C:\Users\me\source\repos` fills with `<repository>-<slug>` folders that the wizard then offers as if they were repositories. The coming open-in-editor key also needs somewhere to say which editor to use.

## What Changes

- `config.json` gains `worktreeRoot` and `editor`. An absent `worktreeRoot` means `<parent of the first repository root>\worktrees` (`C:\Users\me\source\worktrees` by default); an absent `editor` means VS Code. The editor is a preset (VS Code, Cursor, Rider, Visual Studio, Explorer) or a custom executable with arguments holding a `{folder}` placeholder. A `config.json` written by an earlier version reads every missing property as its default.
- **BREAKING (placement only):** new worktrees are created at `<worktreeRoot>\<repository>-<slug>` on branch `<slug>` instead of beside the repository; the worktree root is created when missing. Existing sibling worktrees are untouched and still listed, removed and opened as before.
- The worktrees view's leftover scan (broken registrations and empty `.worktrees` folders) covers the worktree root as well as the repository roots.
- The wizard's repository list offers main checkouts only (a `.git` folder); linked worktrees (a `.git` file) directly under a root are no longer offered as repositories.
- A settings dialog on `,` from both views edits the repository roots (one per line), the worktree root, the editor and the default worktree base, validates them, writes `config.json` and applies them at once without a restart.
- The header gains a `Worktree root:` line on both views.
- `openspec\config.yaml`'s worktree placement line, CLAUDE.md and the README follow the new layout.

Out of scope: the folder column and the copy, open-in-editor and open-solution keys that use the editor setting (`folder-column-and-open-keys`); moving existing sibling worktrees to the new root; a per-repository worktree root; settings for tab colours or Windows Terminal profiles (`windows-terminal-setup`); multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

- `settings`: the user configuration file, its defaults and compatibility with earlier files, the settings dialog, its validation, and applying saved settings without a restart.

### Modified Capabilities

- `session-launch`: a new worktree goes under the worktree root rather than beside the repository; the repository picker offers main checkouts only.
- `worktree-management`: worktrees are listed across the repository roots and the worktree root; leftover detection covers the worktree root.

## Impact

- `Core`: `VantageConfig` gains `WorktreeRoot` and `Editor`, resolved to defaults by a new `Domain\Settings`; new `Domain\EditorChoice` and `CommandArguments`; `SiblingWorktree` becomes `PlannedWorktree` placed by a worktree root; `LaunchPlanBuilder`, `LaunchPreview`, `RepositoryCatalogue`, `WorktreeInventory` read the current settings; new `Application\Views\SettingsState`, `SettingsInput`, `SettingsView`, an `OpenSettings` dialog effect and a `CurrentSettings` holder; new `Wording` entries.
- `Infrastructure`: `JsonFileStore` reads missing config properties as defaults and writes the new ones.
- `Tui`: `SettingsDialog` and `SettingsControls` (binder and layout), a flow in `DialogEffects`, a wider header label column and the `Worktree root:` line; `DialogEffects` and `TableScreen` take the settings holder instead of copied values.
- `Program.cs`: builds the holder from `config.json` once and passes it in place of the copied values.
- Tests: `SettingsShould`, `EditorChoiceShould`, `CommandArgumentsShould`, `CurrentSettingsShould`, `SettingsViewShould`, `SaveSettingsHandlerShould`, `PlannedWorktreeShould` (was `SiblingWorktreeShould`), additions to `LaunchPlanShould`, `LaunchPreviewShould`, `RepositoryCatalogueShould`, `WorktreeInventoryShould`, `WorktreeInventoryLeftoversShould`, `JsonFileStoreShould`, `GitCliIntegrationShould`, `WordingShould`, `WordingOfOutcomesShould`, `DialogEffectsShould`.
- Docs: README (Where Vantage keeps things, the wizard, the settings key), CLAUDE.md, `openspec\config.yaml`. Version: next minor after `main`'s.
