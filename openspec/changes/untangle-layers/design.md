# Design

## Context

See proposal.md for motivation and `docs\architecture-review.md` sections 1 and 2 for the evidence. Today's namespaces: `Domain` (pure), `Adapters.*` (ports and implementations together), `Actions` and `Launch` (use cases), `Screens` (reducers, read models and `Tui`). The dependency rules below are what `split-into-projects` will later turn into project references.

## Goals / Non-Goals

**Goals:** a namespace graph with one direction (`Domain` ← `Ports` ← `Application` ← `Tui` and `Adapters`, with `Program` on top), enforced by a test; `Program.cs` reduced to wiring; no observable change.

**Non-Goals:** renaming or merging the two reducers, changing effect dispatch, introducing value objects, moving any file between projects.

## Decisions

- **Target namespaces.** `Source191.Vantage.Domain` (unchanged); `Source191.Vantage.Ports` for `IClaudeCli`, `IGit`, `ITranscripts`, `IProcessRunner`, `IProcessRegistry`, `IWindowsTerminal`, `IFolders`, the five state ports and the records they carry (`GitWorktreeEntry`, `GitException`, `ProcessRequest`, `ProcessResult`, `TabLaunch`, `TerminalException`, `StartResult`, `StartOutcome`, `ResumeResult`, `VantageConfig`, `TabRegistration`, `CreatedWorktree`); `Source191.Vantage.Application` for `Actions` (including `OwnTab`), `Launch`, `SessionRowEnricher`, `WorktreeInventory`, the reducers and their records; `Source191.Vantage.Adapters.*` for implementations only; `Source191.Vantage.Tui` for `Screens\Tui`. Folder names follow namespaces so the later move is `git mv` of whole folders.
- **`IFolders` is four methods**, no more: `bool Exists(string path)`, `IEnumerable<string> Directories(string path)`, `IEnumerable<string> Lines(string file)`, `bool HasAnyFile(string path, Func<string, bool> except)`. Alternative rejected: System.IO.Abstractions, which brings the whole `System.IO` surface and invites the read models to keep using it. `WorktreeIncludeCopier`, `ClaudeTranscripts` and `FolderDeletion` stay adapters and keep real `System.IO`.
- **`PowerShellScript.Quote` and `Invoke` move to `Application\PowerShellScript`** because they are pure string building used by `TabOpener` and `TrustRecovery`; `Encode` and `WindowsTerminalArguments` stay in the adapter because only `wt.exe` needs them.
- **State ports are one interface per file set,** each returning and taking the existing records. `JsonFileStore` (adapter) implements all five over the folder layout `fix-view-and-state-defects` introduces; a single class implementing five interfaces keeps one JSON options object and one write path. Tests use `InMemoryTabRegistry` and friends in `TestDoubles`. Alternative rejected: one `IStateStore` with fifteen methods, which keeps every consumer coupled to every concern.
- **`ScreenLoop`** owns the alternating `SessionsScreen` and `WorktreesScreen` run, the `switch` on exit effects, the hand-off to `TerminalEffects` and `ConsoleRecovery`, and the own-tab label: shown at start from `OwnTab.ClaimAsync`, rewritten after every hand-off, cleared on quit. The label goes through an `IOwnTabLabel` port (`Show(TabColour?)`, `Clear()`) implemented in `Adapters\Terminal` over `TabEscapes` and `ConsoleRecovery.WriteSequence`, so `TabEscapes` stays an adapter and the loop test asserts the three calls. The loop takes the two screens behind small interfaces (`ISessionsScreen.Run(state)`, `IWorktreesScreen.Run(state)`) so a test can script exits without Terminal.Gui. `HereCommandRunner` wraps `HereCommand.Plan`, `LaunchService.LaunchAsync`, the attach and the exit codes. `CommandLine.Parse(string[] args)` returns `Console`, `Here(args)`, `ClaimTab(id, pid)` or `Usage(message)` as a sealed record hierarchy.
- **The architecture test** lives in `test\...\Architecture\LayeringShould.cs` and asserts: `Domain` references nothing else in the assembly; `Ports` references only `Domain`; `Application` references nothing under `Adapters` or `Tui`; `Adapters` references nothing under `Tui` or `Application`; nothing outside `Tui` and `Program` references Terminal.Gui; nothing outside `Adapters` and `Program` references `System.IO.File`, `System.IO.Directory` or `System.Diagnostics.Process`. Package choice is confirmed through `ctx7` when the task is picked up; both ArchUnitNET and NetArchTest.Rules are current as of 24 October 2026.
- **CLAUDE.md rule wording.** Under Code rules: "Every piece of work, including refactors and tooling, starts as an OpenSpec change (`/opsx:propose`); a change with no behaviour change declares `skip_specs: true`. Archive the change in the same pull request that completes it, so `openspec\specs` always describes the code on `main`." The Build, test, release section also lists the new test folder.

## Risks / Trade-offs

- [Large mechanical diff makes review hard] → one commit per namespace move, with `dotnet build` and tests green at each, and no logic edits in those commits.
- [`InternalsVisibleTo` is already in place for the single project] → nothing to do until the split; noted here so the split change remembers it.
- [`WorktreeInventory` becomes harder to read with `IFolders` threaded through] → the port's method names mirror the `Directory` calls they replace, and the class keeps its private helper structure.
- [Removing NSubstitute while CLAUDE.md's global rule says to include it] → the repository's own CLAUDE.md and `config.yaml` say sociable tests with fakes; the project has never used NSubstitute, and `openspec\config.yaml` is updated to match. The package can return the day a boundary genuinely needs a mock.

## Migration Plan

Each numbered task group is one pull-request-sized step and leaves the build green; the change as a whole can also ship as one pull request.
