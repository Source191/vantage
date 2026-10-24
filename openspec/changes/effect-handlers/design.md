# Design

## Context

See proposal.md. After `generic-table-screen` the two definitions still each carry a `Handle` switch over their effects and a `LeavesScreen` predicate; the screen loop carries a third switch. `SessionActions` has seven public methods and five dependencies; `WorktreeActions` four and three.

## Goals / Non-Goals

**Goals:** one place decides what an effect needs (terminal, dialog, background); one class per effect with two or three dependencies; the composition root is the only place that knows the full set.

**Non-Goals:** a dependency injection container (the handler list is eleven `new` expressions in `Program.cs`), a message bus, changing effect records' fields beyond what the markers need.

## Decisions

- **Markers over a registry of types.** `ILeavesScreen` and `IDialogEffect` are empty interfaces on the effect records in `Core`. The screen loop and `TableScreen` test `effect is ILeavesScreen`; nothing lists concrete types. Alternative rejected: an enum `EffectKind` on `ViewEffect`; markers let pattern matching stay exhaustive and read better in the reducer tests.
- **Handler contract.** `interface IEffectHandler<in TEffect> where TEffect : ViewEffect { Task<string> HandleAsync(TEffect effect, CancellationToken cancellationToken); }`. The return is the footer text until `outcomes-and-wording` replaces it with an outcome record; the interface is generic so that change is a type substitution. Alternative rejected: a non-generic `Handle(ViewEffect)` with a cast; the generic form lets the compiler check every handler against its effect.
- **Dispatcher.** `EffectDispatcher` holds a `Dictionary<Type, Func<ViewEffect, CancellationToken, Task<string>>>` filled by `Register<TEffect>(IEffectHandler<TEffect>)`; `DispatchAsync` throws `InvalidOperationException` naming the effect when no handler is registered, and `EffectDispatcherShould` covers both paths. `Program.cs` registers the eleven handlers. Alternative rejected: reflection over the assembly; explicit registration is eleven lines and visible.
- **Dialog effects stay in Tui but behind one class.** `DialogEffects` in `Tui` has one method per `IDialogEffect` (`Rename`, `ConfirmDelete`, `ConfirmCloseEmpty`, `ConfirmForce`, `ChooseSessionsToResume`, `ShowHelp`), each taking the `EffectContext` and returning either a follow-up `ViewInput` to reduce (`RenameConfirmed`, `ForceConfirmed`) or an effect to dispatch (`DeleteSession` after a yes). `TableScreen` calls `dialogs.Show(effect, context)` and feeds the result back through the reducer or the dispatcher. The "delete then ask about the branch" flow becomes `DeleteSessionHandler` returning an outcome that names a `BranchToConfirm`, which `TableScreen` turns into one more dialog and one more dispatch (`DeleteBranch` becomes an effect with its own handler, used by both views).
- **Handlers are the former action methods, one per class.** `AdoptSessionHandler(IClaudeCli, IProcessRegistry, ITranscripts)`, `RenameSessionHandler(IClaudeCli, ITranscripts)`, `CloseEmptyTerminalsHandler(IProcessRegistry, ITranscripts)`, `StopSessionHandler(IClaudeCli)`, `DeleteSessionHandler(IClaudeCli, IGit, ICreatedWorktrees)`, `DeleteBranchHandler(IGit)`, `ResumeSessionsHandler(IClaudeCli, ITranscripts)`, `OpenTabHandler(ITabRegistry, IProcessRegistry, IWindowsTerminal, TimeProvider, string claudeExecutable, string vantageExecutable)`, `RemoveWorktreeHandler(IGit, ICreatedWorktrees, IFolders)`, `PruneRepositoryHandler(IGit)`, `OpenShellHandler(IWindowsTerminal)`, `StartSessionHandler(LaunchService, OpenTabHandler)`, which runs the wizard's launch from both views and, when `WizardResult.OpenInTab` is set, opens the listed session's tab or reports that it is not listed yet, as `LaunchWorkflow.StartedAsync` does today. `OwnTab` is not an effect and stays as it is. The two tab handlers keep the executable path strings exactly as `TabOpener` and `WorktreeActions` hold them today; `consolidate-helpers` replaces them with the `TabScripts` factory. `SessionActions.PlanResume` becomes a static on `ResumeSessionsHandler`. The tests move case by case; the fixtures (`FakeProcessRunner` rules, `FakeTranscripts`) are shared through a small `SessionFixtures` helper in `Core.Tests`.

## Risks / Trade-offs

- [Eleven small classes are more files than two actions] → each is under sixty lines with two or three dependencies, which is what the object calisthenics rule in CLAUDE.md asks for, and each test class tests one thing.
- [A forgotten registration fails at runtime] → `EffectDispatcherShould.HaveAHandlerForEveryEffectTheReducersCanProduce` enumerates every `ViewEffect` subtype in `Core` that is not a marker type and asserts a registration, using the real `Program` registration factored into `Composition.Handlers(...)`.
- [The branch-confirmation flow spans handler, dialog and a second handler] → it already does today across `RunInBackground` continuations; the new shape just names the steps.
