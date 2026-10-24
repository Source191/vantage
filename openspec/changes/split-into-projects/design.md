# Design

## Context

See proposal.md and `docs\architecture-review.md` section 2. After `untangle-layers` the folders `Domain`, `Ports`, `Application`, `Adapters` and `Tui` map one to one onto namespaces, and `Program.cs` holds only wiring, so each project is a folder move.

## Goals / Non-Goals

**Goals:** project references encode the layering; `Core` builds with no package references; the pure tests run in well under a second with no I/O; the tool still ships as one package.

**Non-Goals:** publishing `Core` or `Infrastructure` as packages, multi-targeting, any change to what the tool does.

## Decisions

- **Four projects, not two.** Two (Core plus host) would stop Core reaching Terminal.Gui and System.IO but would still let Tui code call adapters directly. Four makes `Tui` reference `Core` only, which is the boundary the generic screen and effect handlers rely on later. The cost is three extra csproj files, all tiny.
- **Reference graph.** `Core` → nothing. `Infrastructure` → `Core`, packages Microsoft.Extensions.FileSystemGlobbing, Microsoft.Extensions.Logging.Abstractions and Interop.UIAutomationClient (a ComImport-only library; CLAUDE.md records that it builds clean under warnings-as-errors on net10.0). `Tui` → `Core`, package Terminal.Gui. `Source191.Vantage` (exe) → all three. `Core.Tests` → `Core`. `Infrastructure.Tests` → `Infrastructure` (and `Core` transitively). `HeadlessTerminal.Tests` → `HeadlessTerminal`. The architecture test moves to `Core.Tests` and keeps the intra-assembly rules (Domain, Ports, Application) that project references cannot express.
- **`PackAsTool` stays on the exe.** `dotnet pack` for a tool includes referenced project outputs in the `tools\` folder of the nupkg, so one package still ships. The first task after the split runs `tools\install-local.ps1` and starts `vantage` to prove it.
- **`InternalsVisibleTo`.** `Tui` exposes internals to nothing (its classes are internal and untested by design); `Infrastructure` exposes to `Infrastructure.Tests` for `ConsoleRecovery`'s constants; `Core` exposes to `Core.Tests` only if a later change needs it. `HeadlessTerminal` already exposes to the old test project and switches to its own.
- **Which tests go where.** `Infrastructure.Tests`: `GitCliIntegrationShould`, `GitCliShould`, `GitWorktreeListingShould`, `ClaudeCliShould`, `ClaudeAgentsListingShould`, `ClaudeTranscriptsShould`, `ProcessRunnerIntegrationShould`, `ProcessRegistryIntegrationShould`, `ConsoleRecoveryShould`, `TabEscapesShould`, `FileLoggerShould`, `JsonFileStoreShould`, `WorktreeIncludeCopierShould`, `WindowsTerminalShould`, `WindowsTerminalArgumentsShould`, `PowerShellScriptShould` (the `Encode` part), `FoldersShould`, plus `TemporaryGitRoot` and `FakeProcessRunner` (the fake tests process requests, which live in `Core.Ports`, so it can sit in either; it goes with the integration tests that need it most). Everything else is `Core.Tests`. `ScaffoldShould` is deleted or becomes the solution-level smoke test in `Core.Tests`.
- **Central package management.** `Directory.Packages.props` at the root with `ManagePackageVersionsCentrally`; each csproj lists package names only. `Directory.Build.props` keeps the analyser and warnings-as-errors settings unchanged.
- **CI.** `check.yml` runs `dotnet test` at the solution so all three test projects run; the `pack` step names `src\Source191.Vantage`. The `handoff` job is unchanged. Stryker scoping in CLAUDE.md becomes `--project src\Source191.Vantage.Core --mutate "**/Domain/*.cs"`.

## Risks / Trade-offs

- [The tool package misses a referenced assembly] → verified by the install-and-run task before anything else is touched; rollback is reverting one commit because the split is a pure move.
- [`FakeProcessRunner` needed by both test projects] → if `Core.Tests` turns out to need it, a tiny `Source191.Vantage.TestDoubles` project is the fallback; not created until needed.
- [Three test projects slow `dotnet test`] → the pure project is the one run in the inner loop; CLAUDE.md names it.

## Migration Plan

Tasks run in order on one branch; each group is a commit with a green build. The change ships as one pull request because a half-split solution is worse than either state.
