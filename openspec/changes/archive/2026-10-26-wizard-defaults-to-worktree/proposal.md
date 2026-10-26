# Proposal

## Why

Most sessions started from the overview's `n` key are pieces of work that belong in their own worktree, yet the wizard opens on "Current branch", so the user has to remember to switch Where every time, and forgetting it starts the session in the main checkout. Making "New worktree" the default matches how the wizard is actually used.

## What Changes

- The new-session wizard opened with `n` starts with Where on "New worktree"; the arrows still switch to "Current branch". The preview reflects the worktree from the moment the wizard opens.
- The Where control shows the mode the wizard state opens with, rather than always drawing "Current branch" first.
- Choosing a folder that is not a repository (including "(no repository)") with the default left in place behaves as choosing "New worktree" by hand does today: the preview says a worktree needs a repository and Start refuses until the user switches to "Current branch".
- Unchanged: the worktrees view's `s` key (the wizard on a fixed folder, which has no Where choice and starts on that folder's branch), `vantage here` (always the current folder and branch), the worktree base default (still from the settings) and how a worktree is named and placed.
- The project context in `openspec\config.yaml` stops saying the current branch is the default launch mode.
- **Behaviour change, not breaking:** accepting the wizard's defaults now creates a worktree. Raised as a minor version (0.13.0), since no key, state file or command line changes.

Out of scope: a setting to choose the default Where (a fixed default for now, per the user), changing `vantage here` to make worktrees, remembering the last Where choice between openings, and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: the requirement "The current branch is the default launch mode" is renamed "A new worktree is the default launch mode" and rewritten for the wizard opened with `n`, with scenarios for choosing the current branch, the fixed-folder wizard and for a non-repository choice keeping the default and being refused on Start.

## Impact

- `src\Source191.Vantage.Core\Application\Views\WizardState.cs` (`Open` chooses the mode), `src\Source191.Vantage.Tui\WizardControls.cs` (the Where selector's initial value follows the state).
- Tests: `WizardViewShould` facts that assumed the current-branch default.
- `README.md` (the wizard's Where line and the worked example), `openspec\config.yaml` context, `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` raised to 0.13.0.
- No change to ports, adapters, state files or the command line.
