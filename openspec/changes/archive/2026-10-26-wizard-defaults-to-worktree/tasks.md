# Tasks

## 1. The wizard's opening mode (Application\Views)

- [x] 1.1 Red: change `WizardViewShould.OpenWithRecentRepositoriesFirstAndTheFirstChoiceSelected` to expect `LaunchMode.NewWorktree`, and `PreviewFollowsNameModeAndSelection` to expect the worktree preview for `InAgents` (switching to `CurrentBranch` for the current-branch line); run the Core suite and see both fail on the mode, not on anything else
- [x] 1.2 Green: `WizardState.Open` sets `NewWorktree` when there is no fixed folder and `CurrentBranch` when there is; the Core suite passes, including `FixedDirectoryStartsInThatFolder` unchanged
- [x] 1.3 Add `WizardViewShould.StartWithTheDefaultsMakesAWorktree` (name only, no mode input, Start gives a plan with a worktree under the worktree root) and `StartWithTheDefaultsRefusesAFolderThatIsNotARepository` ("no repository" chosen, Start refuses with `LaunchProblem.WorktreeNeedsRepository` and focus on the name); both pass, matching the spec's scenarios
- [x] 1.4 Refactor: simplify the `ModeChanged(NewWorktree)` steps in `Fill` and `TakeTheWorktreeRootAndBaseFromTheSettings` that the default now makes redundant, keeping the tests readable; `dotnet build -c Debug` and the Core suite pass

## 2. The Where control (Tui)

- [x] 2.1 `WizardControls` takes the Where selector's initial `Value` from `state.Mode` (as `BaseRef` takes `state.Base`), with a switch naming both `LaunchMode` members; build, then check with `dotnet run --project tools\HeadlessTerminal -- drive` that `n` opens with "New worktree" selected and the worktree preview, and `check` exits 0

## 3. Documentation and release

- [x] 3.1 README: the wizard's Where line says `New worktree` is the default and `Current branch` the alternative, and the "Start work on a story" example and the manual check drop "pick `New worktree`"; read back to confirm no other text claims the current branch is the default
- [x] 3.2 `openspec\config.yaml` context: replace "Same branch is the default launch mode; a worktree is an explicit choice." with the new default; `openspec validate wizard-defaults-to-worktree --strict` passes
- [x] 3.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to 0.13.0; `.github\scripts\Assert-VersionBumped.ps1` passes against `main`

## 4. Verification

- [x] 4.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` all pass

## Workflow follow-up

- Archive the change in the same pull request, so `openspec\specs\session-launch\spec.md` carries the renamed requirement.
