# Tasks

## 1. Markers and dispatcher

- [ ] 1.1 `SessionsViewShould.MarkEffectsThatLeaveTheScreen` and `WorktreesViewShould.MarkEffectsThatLeaveTheScreen`: attach, agent view, quit, switch and jump implement `ILeavesScreen`; add the markers (`ILeavesScreen`, `IDialogEffect`) and apply them
- [ ] 1.2 `EffectDispatcherShould.RouteAnEffectToItsHandler` and `RefuseAnUnregisteredEffectByName`: implement `IEffectHandler<TEffect>` and `EffectDispatcher`

## 2. Session handlers

- [ ] 2.1 `StopSessionHandlerShould`: move the stop case from `SessionActionsShould`; implement `StopSessionHandler`
- [ ] 2.2 `AdoptSessionHandlerShould`: move the adopt cases; implement `AdoptSessionHandler`
- [ ] 2.3 `RenameSessionHandlerShould`: move the rename cases including the restore-under-old-name path; implement `RenameSessionHandler`
- [ ] 2.4 `CloseEmptyTerminalsHandlerShould`: move the close cases including the summary wording; implement `CloseEmptyTerminalsHandler`
- [ ] 2.5 `DeleteSessionHandlerShould` and `DeleteBranchHandlerShould`: move the delete and branch cases; implement both handlers with `DeleteBranch` as a new effect
- [ ] 2.6 `ResumeSessionsHandlerShould`: move the resume and plan cases; implement `ResumeSessionsHandler`
- [ ] 2.7 `OpenTabHandlerShould`: rename `TabOpenerShould`, keeping the open and switch-to-live-tab cases; `TabOpener` becomes `OpenTabHandler`
- [ ] 2.8 Delete `SessionActions` and `SessionActionsShould`; verify `dotnet build` and tests pass

## 3. Worktree handlers

- [ ] 3.1 `RemoveWorktreeHandlerShould`: move the removal cases from `WorktreeActionsShould`; implement `RemoveWorktreeHandler`
- [ ] 3.2 `PruneRepositoryHandlerShould` and `OpenShellHandlerShould`: move the cases; implement both
- [ ] 3.3 `StartSessionHandlerShould`: the `StartedAsync` cases from `LaunchWorkflowShould` (started, started and opened in a tab, started but not listed yet, untrusted) plus starting from a worktree row with the fixed directory plan; implement `StartSessionHandler` and both wizards call it
- [ ] 3.4 Delete `WorktreeActions` and `WorktreeActionsShould`; verify build and tests pass

## 4. Screens route only

- [ ] 4.1 `DialogEffects` in `Tui` gathers the six dialog flows from the two definitions; `TableScreen` dispatches `IDialogEffect` to it and everything else to `EffectDispatcher`; the definitions lose their `Handle` members; verify `dotnet run --project tools\HeadlessTerminal -- check` passes
- [ ] 4.2 `ScreenLoopShould` uses `ILeavesScreen` instead of a type list; remove the type lists from `TableScreen` and `ScreenLoop`; verify the test passes
- [ ] 4.3 `EffectDispatcherShould.HaveAHandlerForEveryEffectTheReducersCanProduce`: factor the registrations into `Composition.Handlers(...)` used by `Program.cs` and the test; verify it passes
- [ ] 4.4 Manual pass through both README key tables (every key, including rename, delete with branch question, force remove, resume checklist, close empty); verify no footer text changed

## 5. Documentation

- [ ] 5.1 Update CLAUDE.md Code rules with the recipe for a new key (effect record, reducer case, handler, registration) and the `openspec\config.yaml` tasks rule to name handlers after adapters and before screens; verify `openspec validate` passes for every open change

## Workflow follow-up

- PR 10: task groups 1 and 2 (markers, dispatcher, the eight session handlers, `SessionActions` deleted).
- PR 11: task groups 3, 4 and 5 (worktree handlers, `DialogEffects`, screens route only, registration completeness test). Archive the change in this PR.
