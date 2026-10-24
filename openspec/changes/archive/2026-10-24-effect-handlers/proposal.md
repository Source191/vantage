# Proposal

## Why

Which effects leave a screen is hard-coded twice (`SessionsScreen.cs:100` and `WorktreesScreen.cs:102`) and dispatched a third time in the screen loop; the in-screen effects are a `switch` inside each screen that calls into `SessionActions`, `WorktreeActions`, `TabOpener` and `LaunchWorkflow`, so the screens hold eight constructor arguments between them and every new key edits a screen. Classifying effects on the effect itself and giving each one a handler makes a new key an effect record, a reducer case and a handler class, with no screen change.

## What Changes

- Marker interfaces on effects: `ILeavesScreen` (needs the terminal or another screen: attach, agent view, quit, switch, jump) and `IDialogEffect` (needs a dialog before anything runs: rename, force-confirm, delete confirmation, close-empty confirmation, resume checklist, help). Everything else runs in the background and updates the footer.
- `IEffectHandler<TEffect>` in `Application` for background effects, returning an outcome string (or the outcome record once `outcomes-and-wording` lands); one class per effect: `OpenTabHandler`, `StopSessionHandler`, `DeleteSessionHandler`, `AdoptSessionHandler`, `CloseEmptyTerminalsHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`, `RemoveWorktreeHandler`, `PruneRepositoryHandler`, `OpenShellHandler`, `StartSessionHandler`.
- An `EffectDispatcher` in `Application` that routes an effect to its handler by type, built in the composition root from the handler instances.
- `SessionActions` and `WorktreeActions` dissolve into the handlers; `OpenTabHandler` keeps both of `TabOpener`'s paths (open a new tab, or switch to the live one through `IWindowsTerminal.FocusTabAsync`); `StartSessionHandler` takes over `LaunchWorkflow.StartedAsync`, opening the new session's tab through `OpenTabHandler` when the wizard asked for it. The dialog effects keep their Terminal.Gui code in `Tui` but go through one `DialogEffects` class instead of two switches.
- Screens take the dispatcher and the dialog effects instead of the actions and the launch workflow.

Out of scope: changing any key or footer, the wizard, outcome records (next change), multiple Claude accounts. Depends on `generic-table-screen`.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. Structure only; `.openspec.yaml` declares `skip_specs: true`.

## Impact

- `Core`: `Application\Effects\*` (markers, handler interface, dispatcher, eleven handlers); `SessionActions.cs` and `WorktreeActions.cs` removed; `TabOpener` becomes `OpenTabHandler`.
- `Tui`: `DialogEffects.cs`; the two screen definitions lose their `Handle` switches.
- Tests: `SessionActionsShould` and `WorktreeActionsShould` split into one test class per handler with the same cases; `EffectDispatcherShould` is new.
- `Program.cs`: builds the dispatcher; constructor argument counts on the screens drop to three.
