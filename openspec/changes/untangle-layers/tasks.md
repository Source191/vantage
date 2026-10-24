# Tasks

## 1. Lock the direction

- [ ] 1.1 Confirm the architecture-test package through `npx ctx7@latest library` (ArchUnitNET or NetArchTest.Rules) and add it to the test project; verify `dotnet build -c Debug` passes
- [ ] 1.2 `LayeringShould.KeepDomainFreeOfEverythingElse`: Domain types reference no other namespace in the assembly; verify it passes today (red is a sign of a mistake in the rule)
- [ ] 1.3 `LayeringShould.KeepTerminalGuiInsideTui`, `KeepFileSystemAndProcessesInsideAdapters`, `KeepApplicationFreeOfAdaptersAndTui`, `KeepAdaptersFreeOfApplicationAndTui`: write all four, mark the ones that fail today with `Skip` naming the task that fixes them, and unskip each as its task lands

## 2. Application namespace

- [ ] 2.1 Move `SessionRow`, `WorktreeRow`, `Key`, `KeyName`, `ViewInput`, `ViewEffect` and all effect records, `SessionsView`, `SessionsViewState`, `WorktreesView`, `WorktreesViewState` to `Application\Views`; verify build, tests and format pass with no test edits beyond `using`
- [ ] 2.2 Move `SessionRowEnricher`, `WorktreeInventory`, `StateTransitionNotifier`, `Actions\*` and `Launch\*` to `Application`; verify build, tests and format pass
- [ ] 2.3 Unskip `KeepApplicationFreeOfAdaptersAndTui` once 3.x lands; until then verify it fails only on `Adapters` references

## 3. Ports namespace

- [ ] 3.1 Move `IClaudeCli`, `StartResult`, `StartOutcome`, `ResumeResult`, `ITranscripts` to `Ports`; verify build and tests pass
- [ ] 3.2 Move `IGit`, `GitWorktreeEntry`, `GitException`, `IProcessRunner`, `ProcessRequest`, `ProcessResult`, `IWindowsTerminal`, `TabLaunch`, `TerminalException`, `IProcessRegistry`, `ProcessParent` to `Ports`; move `PowerShellScript.Quote` and `Invoke` to `Application\PowerShellScript` leaving `Encode` with the adapter; verify build and tests pass
- [ ] 3.3 `HandoffLogShould.LogTheEffectName`: `HandingOver` takes a string; change the signature and the `Program.cs` call
- [ ] 3.4 `FoldersShould` (integration, temp folder): `Exists`, `Directories`, `Lines`, `HasAnyFile` behave like `System.IO`; define `IFolders` in `Ports` and `Folders` in `Adapters\Files`
- [ ] 3.5 `WorktreeInventoryShould` uses a new `FakeFolders` instead of temp folders for the orphan, broken and empty cases; thread `IFolders` through `WorktreeInventory` and make the existing tests pass unchanged in intent
- [ ] 3.6 `RepositoryCatalogueShould` uses `FakeFolders`; thread `IFolders` through `RepositoryCatalogue` and `LaunchWorkflow.Plan`
- [ ] 3.7 Unskip `KeepFileSystemAndProcessesInsideAdapters` and verify it passes

## 4. State ports

- [ ] 4.1 Define `ITabRegistry`, `IRecentRepositories`, `ICreatedWorktrees`, `ISessionDescriptions`, `IVantageConfig` in `Ports` with the existing method shapes; `JsonFileStore` in `Adapters\State` implements all five; `StateStoreShould` is renamed `JsonFileStoreShould` and passes unchanged in intent
- [ ] 4.2 Add `InMemoryTabRegistry`, `InMemoryRecentRepositories`, `InMemoryCreatedWorktrees`, `InMemorySessionDescriptions` to `TestDoubles`, each with a one-fact test
- [ ] 4.3 `TabOpenerShould`, `OwnTabShould`, `SessionActionsShould`, `WorktreeActionsShould`, `LaunchServiceShould`, `LaunchWorkflowShould`, `SessionRowEnricherShould` take the in-memory fakes and no longer create temp folders; verify they pass and that `GetTempPath` appears only in adapter tests and `TemporaryGitRoot`
- [ ] 4.4 Unskip `KeepApplicationFreeOfAdaptersAndTui` and verify it passes

## 5. Program becomes wiring

- [ ] 5.1 `CommandLineShould.ParseConsoleHereClaimTabAndUsage`: the four argument shapes parse to the four records, including the `claim-tab` Guid and pid validation; implement `CommandLine.Parse`
- [ ] 5.2 `HereCommandRunnerShould.StartAttachAndReturnTheExitCodes`: trusted start returns 0 after attach, untrusted returns 1 with the trust message, bad arguments return 2 with usage, all against `FakeProcessRunner`; implement `HereCommandRunner`
- [ ] 5.3 `ScreenLoopShould.AlternateScreensAndRunTerminalEffects` and `LabelTheOwnTabAtStartAfterHandOffsAndClearItOnQuit`: scripted `ISessionsScreen` and `IWorktreesScreen` doubles drive quit, switch both ways, jump to a session (as a `SelectSession` input) and an attach hand-off, with a `FakeOwnTabLabel` recording show and clear calls; define `IOwnTabLabel` in `Ports` with its `TabEscapes` implementation in `Adapters\Terminal`; implement `ScreenLoop` and make `SessionsScreen` and `WorktreesScreen` implement the two interfaces
- [ ] 5.4 Reduce `Program.cs` to construction and three calls; verify `dotnet build`, tests, `dotnet format --verify-no-changes` and `dotnet run --project tools\HeadlessTerminal -- check` pass

## 6. Housekeeping

- [ ] 6.1 Remove NSubstitute and NSubstitute.Analyzers.CSharp from the test project; add Microsoft.Extensions.Logging.Abstractions to the tool project; verify `dotnet build -c Release` passes with no new warnings
- [ ] 6.2 Update `openspec\config.yaml` context (test packages; add the layering rule in one sentence) and CLAUDE.md (OpenSpec rule, namespace map, architecture test in the test loop); verify `openspec validate` passes for every open change
- [ ] 6.3 Update the README developer section with the namespace map; verify the described commands run as written

## Workflow follow-up

- PR 3: task groups 1 and 2 (architecture test package and rules, `Application` namespace move). Commit the move separately from any edit so the review shows renames. Needs `fix-view-and-state-defects` PR 1 merged, not PR 2.
- PR 4: task group 3 (`Ports` namespace, `IFolders`, `HandoffLog`). Move commit first, edits second.
- PR 5: task group 4 (state ports and fakes). Needs `fix-view-and-state-defects` PR 2 merged.
- PR 6: task groups 5 and 6 (`ScreenLoop`, `HereCommandRunner`, `CommandLine`, package tidy, CLAUDE.md and config.yaml). Archive the change in this PR. No version bump for the change.
