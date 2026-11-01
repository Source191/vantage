# Proposal

## Why

Four nulls are left in the models after the first six changes of the null programme. Each stands for a state the code already knows about but has no name for: a background job with no process (`BackgroundSession.ProcessId int?`), a launch on the current branch (`LaunchPlan.Worktree?`, with a `ReuseExistingWorktree` flag that means nothing when it is null), a session outside any repository (`SessionRow.Checkout?` and the enricher's private `Location`), and a wizard opened on a fixed folder (`WizardState.FixedChoice?` and the `FolderPath? fixedDirectory = null` threaded through the Tui). The owner deferred these to the end of the programme on 31 October 2026 because each reshapes a record that reducers, key gating or labelling read directly, so each is planned to land green on its own.

## What Changes

- **`JobProcess`** (Domain): `Running(int Id)` or `None`. `BackgroundSession` takes a required `JobProcess Process` in place of `int? ProcessId = null`; `ClaudeAgentsListing` turns the listing's `pid` into one on the line that reads it. Key gating (`SessionsView`, `WordingOfConsole.SessionsBar`), the memory key, rename's "never sent a message" check, the live-session filter in `WorktreeInventory` and `SessionRowEnricher.MemoryOf` match the case.
- **`LaunchPlace`** (Application, `Launch`): `CurrentBranch`, `NewWorktree(PlannedWorktree)` or `ExistingWorktree(PlannedWorktree)`. `LaunchPlan` takes a `LaunchPlace Place` in place of `PlannedWorktree? Worktree` and `bool ReuseExistingWorktree`; `LaunchPlanBuilder` chooses the case and `LaunchService` creates a worktree only for `NewWorktree`.
- **`SessionLocation`** (Application, `Views`): `Outside(string Folder)` or `InRepository(string Name, FolderPath Checkout)`. `SessionRow` takes a `SessionLocation Location` in place of its positional `string Repository` and its `FolderPath? Checkout`, and keeps a `Repository` member that answers the location's name, so its five readers do not change. The enricher's private `Location` record goes; the head (a `Head` since null-free-rows) stays its own member of the row.
- **`WizardOrigin`** (Application, `Views`): `Anywhere` or `Fixed(RepositoryChoice)`. `WizardState` takes a `WizardOrigin Origin` in place of `RepositoryChoice? FixedChoice`; `WizardState.Open` and `LaunchWizardDialog.Show` take a `WizardOrigin` in place of `FolderPath? fixedDirectory = null`, and `DialogEffects` passes `Anywhere` for `n` and `Fixed` for `s`. `WizardState.Chosen` answers a `RepositoryPick` (`None` or `Picked(RepositoryChoice)`) in place of `RepositoryChoice?`.
- **Version:** one step above main when it merges (projected `0.19.0-preview.11`).
- `skip_specs: true`: no behaviour changes. Every key, footer, label, title, preview, key bar, memory figure and pull-request lookup reads as before.

Out of scope: `WizardState.SelectedIndex int?`, which stays because it is the shape of Terminal.Gui's `ListView.SelectedItem` it is bound to (`LaunchWizardDialog`), and a value type; `InteractiveSession.ProcessId`, already non-nullable; `ProcessIdentity.StillTheSession(DateTimeOffset?)` and `IProcessRegistry`'s reads, which are the operating system's "gone or refused" and stay as the survey decided; `ClaudeStates.From(int? processId, ...)` and the listing's DTO, which null-at-the-boundary confines; `LaunchOutcome`, the wizard's dialog result and the opening prompt, which null-free-outcomes and null-at-the-boundary own; `SessionRow`'s head, live tab and memory, which null-free-rows owns; the sibling changes shared-snapshot, null-suppressions, null-free-outcomes, null-free-rows, null-free-screens, null-at-the-boundary and null-guard-rule; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code (Core):** new `Domain\JobProcess.cs`, `Application\Launch\LaunchPlace.cs`, `Application\Views\SessionLocation.cs`, `Application\Views\WizardOrigin.cs` and `RepositoryPick` beside it; `Domain\Session.cs`, `Application\Launch\LaunchPlan.cs`, `LaunchService.cs`, `HereCommand.cs`, `Application\SessionRowEnricher.cs`, `Views\SessionsView.cs`, `Views\WizardState.cs`, `Views\WizardView.cs`, `WorktreeInventory.cs`, `WordingOfConsole.cs`, `WordingOfDetail.cs`, `Wording.cs` (wizard title and chosen line), `Detail\DetailLoader.cs`.
- **Code (adapters and Tui):** `Infrastructure\Claude\ClaudeAgentsListing.cs`; `Tui\SessionsScreenDefinition.cs`, `Tui\DialogEffects.cs`, `Tui\LaunchWizardDialog.cs`.
- **Tests:** about 16 `BackgroundSession` constructions and five `with { ProcessId = ... }`; about 16 `LaunchPlan` lines; 13 `SessionRow` constructions and 10 `Checkout` lines; 27 wizard lines, mostly `WizardViewShould` (its ten `Chosen!` become a case match). Row and plan builders (`SessionTestData`, `WorktreeTestData`) absorb most of it.
- **Docs:** `CLAUDE.md` where it names a session's kinds and the wizard's fixed folder; `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

Change 9 of the null programme, which merges one change after another: 1 shared-snapshot (0.19.0-preview.3), 2 null-suppressions (preview.4), 3 null-free-outcomes (preview.5), 4 null-free-rows (preview.6), 5 null-free-observations (preview.7), 6 null-free-screens (preview.8), 7 null-at-the-boundary (preview.9), 8 null-free-port-answers (preview.10), 9 null-free-sessions (this one, preview.11), 10 null-guard-rule (no version). Each version is one step above main when it merges, so the numbers shift if anything else lands in between. This change builds on null-free-rows' `Head` on `SessionRow` and in the enricher, null-free-outcomes' `LaunchOutcome` and wizard dialog answer, and null-at-the-boundary's opening prompt value on `LaunchPlan`.
