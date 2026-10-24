# Proposal

## Why

The single project has dependency cycles that make a project split impossible: `Actions` imports `Screens` for the row and effect records while `Screens\Tui` imports `Actions`; the ports live beside their adapters under `Adapters.*`; `StateStore` is a concrete class with five jobs and no port, used by seven classes including the new `OwnTab`; `HandoffLog` in an adapter takes a `ViewEffect`; and `Program.cs` holds a screen loop, the own-tab title and colour writes, and the `vantage here` command that CLAUDE.md says should be tested code. This change straightens the dependencies inside the one project so that the physical split (`split-into-projects`) is a file move with no code changes.

## What Changes

- Row, key, input and effect records and the two reducers move from `Screens` to an `Application` namespace; `SessionRowEnricher` and `WorktreeInventory` move there too. `Actions` no longer references anything the Tui owns.
- The five boundary interfaces (`IClaudeCli`, `ITranscripts`, `IGit`, `IProcessRunner`, `IWindowsTerminal`) and the records they return or throw move to a `Ports` namespace next to `IProcessRegistry`.
- A narrow `IFolders` port (exists, enumerate directories, read lines, any files) replaces direct `Directory` and `File` calls in `WorktreeInventory`, `RepositoryCatalogue` and `LaunchWorkflow`; `ClaudeExecutable.Locate` is file-system code and is called only from `Program.cs`.
- `StateStore` becomes five small stores behind ports (`ITabRegistry`, `IRecentRepositories`, `ICreatedWorktrees`, `ISessionDescriptions`, `IVantageConfig`) sharing one JSON file helper, with in-memory fakes in the tests.
- `HandoffLog.HandingOver` takes a string.
- The screen loop leaves `Program.cs` for a tested `ScreenLoop`, which also owns the own-tab label (written at start, cleared on quit, rewritten after each hand-off) through a small port; `vantage here` gets a tested `HereCommandRunner`; argument parsing becomes `CommandLine.Parse` returning a small union; `Program.cs` is wiring only.
- One architecture test asserts the dependency direction, so none of the above can regress before or after the split.
- Housekeeping found on the way: the unused NSubstitute and NSubstitute.Analyzers.CSharp packages are removed from the test project, Microsoft.Extensions.Logging.Abstractions is referenced explicitly instead of arriving through Terminal.Gui, `openspec\config.yaml` stops naming NSubstitute, and CLAUDE.md gains a rule that every piece of work starts as an OpenSpec change.

Out of scope: the physical project split, the generic table screen, effect handlers, value objects, the wizard rewrite, multiple Claude accounts. No behaviour changes: every footer sentence, key and column stays as it is. Depends on `fix-view-and-state-defects`, which introduces the one-record-per-file state layout the state ports wrap and the `SelectSession` input the screen loop reduces.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This change alters structure only, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- Every file under `src\Source191.Vantage` except `Domain\*` changes namespace or imports; `test\Source191.Vantage.UnitTests` follows.
- New test doubles: `FakeFolders` and one fake per state-store port. Eight test classes stop creating temp folders.
- Packages: remove NSubstitute and NSubstitute.Analyzers.CSharp; add Microsoft.Extensions.Logging.Abstractions to the tool project and one architecture-test package (ArchUnitNET or NetArchTest.Rules) to the test project.
- Documentation: CLAUDE.md (OpenSpec rule, namespace map), `openspec\config.yaml` (test packages, layering rule), README developer section.
