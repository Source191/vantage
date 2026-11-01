# Tasks

Each task is one red-green-refactor cycle or smaller, with a commit per step; the fact is named before the code. Verify with `dotnet build -c Debug` (warnings are errors) and `dotnet test --project test\Source191.Vantage.Core.Tests -c Debug --no-build` unless a task says otherwise; a task that touches an adapter also runs `dotnet test --project test\Source191.Vantage.Infrastructure.Tests -c Debug --no-build`. Fixtures use Northwind.* names and `C:\Users\me` paths. Groups 1 to 4 can start now; group 5 waits for `listed-name` and `process-registry-one-path` to reach `main` where it touches `IClaudeCli`; group 6 waits for `editor-not-found`.

## 1. The folder deleter

- [ ] 1.1 `LayeringShould.HandTheFolderDeleterOnlyToWorktreeRemover`: the types in Core, Infrastructure, Tui and TestDoubles that depend on `Source191.Vantage.Ports.IFolderDeleter` are exactly `WorktreeRemover`, `Adapters\Files\Folders` and `FakeFolders`. Red (the type does not exist), then green with `src\Source191.Vantage.Core\Ports\IFolderDeleter.cs` (one member, `DeleteTree`'s doc moved from `IFolders`), `DeleteTree` removed from `IFolders`, `Folders : IFolders, IFolderDeleter`, `FakeFolders : IFolders, IFolderDeleter`, and `WorktreeRemover(IGit, ICreatedWorktrees, IFolderDeleter, CurrentSettings)`. The build fails at `RemoveWorktreeHandler.cs:17` and `DeleteSessionHandler.cs:15`, which 1.2 fixes in the same commit. Verify the fact passes.
- [ ] 1.2 `RemoveWorktreeHandler` and `DeleteSessionHandler` take a `WorktreeRemover` as their last constructor parameter and drop their `remover` fields; `RemoveWorktreeHandlerShould` and `DeleteSessionHandlerShould` build one `WorktreeRemover` over the same `FakeFolders` and pass it. Verify every removal and deletion fact still passes and `SealedTypesShould` stays green.
- [ ] 1.3 Wiring: the line that builds `DeleteSessionHandler` and `RemoveWorktreeHandler` for `DialogEffects` (`Program.cs:149`, or `ConsoleComposition` after `tidy-composition-root`) builds one `WorktreeRemover(git, store, folders, settings)` and hands it to both. Verify `dotnet build -c Debug` and `dotnet test -c Debug --no-build` at the solution.
- [ ] 1.4 Docs: `CLAUDE.md`'s security line and `.claude\rules\security.md`'s "Deleting folders" say a folder is deleted only through `IFolderDeleter`, held by `WorktreeRemover` alone (`LayeringShould`), and the layers paragraph names the port. Verify by reading each against design.md; no em dash introduced.

## 2. The one Domain interface

- [ ] 2.1 `LayeringShould.DeclareNoDomainInterfaceButTheTwoNamed`: the interfaces in `Source191.Vantage.Domain` are exactly `ITextValue<>` and `IProcessRegistry`, each beside a one-line reason in the test. Verify the fact passes (green at once; say so in the commit) and that adding a probe interface to the expected list makes it fail for the right name.
- [ ] 2.2 Docs: `CLAUDE.md`'s layers line says `Domain` declares `IProcessRegistry`, the one boundary interface its rules read, held by `LayeringShould`; `LayeringShould`'s summary says the same. Verify by reading against design.md.

## 3. Claude Code's executable

- [ ] 3.1 `ClaudeExecutableShould.NotSearchARelativePathEntryForTheShim`: with PATH folders `["npm", @"C:\Tools"]` and a `FakeFolders` holding `Path.GetFullPath(@"npm\claude.cmd")` (the cwd-relative file the entry would name) and nothing else, `Locate` answers `Missing`. Red, then green with `PathFolders.Searchable(pathFolders)` on the batch-file search in `ClaudeExecutable.cs:33`. Verify the fact and the five existing `ClaudeExecutableShould` facts pass.

## 4. Domain values at the ports

- [ ] 4.1 `PortParametersShould.TakeDomainValues` in `test\Source191.Vantage.Core.Tests\Architecture`: over every interface in `Source191.Vantage.Ports`, no parameter named `sessionId`, `jobId`, `directory`, `repository`, `worktree` or `branch` is a `string`, unless its interface is on `Kept` with a reason; `KeepNoStaleEntry` fails for a kept interface with no such parameter; `FindTheProbe` passes a nested probe interface and expects its member named. Red for `IClaudeCli`, `ITranscripts`, `IWorktreeIncludeCopier`, `IRecentRepositories` and `IGit`; `IGit` goes on `Kept` ("seventeen methods and about forty call sites; migrates in its own change") in this task, the other four turn green in 4.2 to 4.5. Verify the fact names exactly those four interfaces.
- [ ] 4.2 `ITranscripts.HasMessages(SessionId sessionId)`: `ClaudeTranscripts` reads `.Value`; `FakeTranscripts.WithMessages` takes a `SessionId`; `ClaudeTranscriptsShould` and the five callers (`AdoptSessionHandler.cs:21`, `CloseEmptyTerminalsHandler.cs:24`, `RenameSessionHandler.cs:20`, `ResumeSessionsHandler.cs:22`, `SessionRowEnricher.cs:30`) drop `.Value`. Verify both test projects pass.
- [ ] 4.3 `IWorktreeIncludeCopier.CopyAsync(FolderPath repository, FolderPath worktree, ...)`: `WorktreeIncludeCopier` reads `.Value` for git; `LaunchService.cs:54` passes the values it holds; `WorktreeIncludeCopierShould` builds `FolderPath`s. Verify both test projects pass.
- [ ] 4.4 `IRecentRepositories` takes and gives `FolderPath`: `InMemoryState` holds `List<FolderPath>` and removes an equal path before inserting; `LaunchService.cs:40` passes `plan.Repository.Path`; `RepositoryCatalogue.Choices(IReadOnlyList<FolderPath> recent, string filter)` stops parsing the recent list; `WizardState.Open` and `LaunchWizardDialog.cs:27-28` carry the new type; `InMemoryStateShould`, `LaunchServiceShould` and the wizard facts build `FolderPath`s. Verify the Core facts pass.
- [ ] 4.5 `JsonFileStoreShould.KeepOneEntryForEverySpellingOfAFolder`: recording `FolderPath.From(@"C:\repos\A\")`, `FolderPath.From(@"C:/repos/A")` and `FolderPath.From(@"c:\repos\a")` leaves one file under `recent\` and one entry read back, the newest spelling. Red (the port still takes strings here), then green: `JsonFileStore`'s `RecentRepository(FolderPath Path, DateTimeOffset UsedAt)`, `RecentFile(FolderPath)` keyed on `repository.Key`, `ImportLegacyRecentAsync` keeping `Parsed<FolderPath>()` of the old list. Verify the fact and the existing recent facts (`KeepRecentRepositoriesMostRecentFirstWithoutDuplicates`, `KeepOnlyTheTenMostRecentRepositories`, the two-process fact, the legacy import) pass with `FolderPath` fixtures.
- [ ] 4.6 `JsonFileStoreShould.ReadDistinctFoldersNewestFirst` and `SkipARecentFileHoldingARelativePath`: two files under `recent\` for one folder (one written by hand under another name) read as one entry, and a hand-written file holding `"repos\A"` reads as absent beside a good one. Red, then green with `DistinctBy` on the read. Verify the facts pass.
- [ ] 4.7 Docs: `CLAUDE.md`'s values line reads "a port takes the Domain value where one exists (`SessionId`, `JobId`, `FolderPath`, `BranchName`, `SessionName`); adapters read `.Value` (`PortParametersShould`; `IGit` migrates in its own change)". Verify by reading against design.md.

## 5. `IClaudeCli` takes the values (after `listed-name` and `process-registry-one-path`)

- [ ] 5.1 Rebase onto `main` with `listed-name` merged and read the `IClaudeCli` it leaves (one or two resume members, a `ListedName`). Verify `dotnet build -c Debug` passes before changing anything.
- [ ] 5.2 `ClaudeCliShould`'s start, attach, stop, remove and resume facts pass `FolderPath.From(Directory)`, `JobId.From(...)` and `SessionId.From("81cdb0f9-8c3f-4c9b-b478-ca0a5ebf85c6")`, asserting the same argument lists as today. Red (the signatures take strings), then green: `IClaudeCli.StartBackgroundAsync(FolderPath directory, ...)`, `AttachAsync(JobId)`, `StopAsync(JobId)`, `RemoveAsync(JobId)`, `ResumeBackgroundAsync(SessionId, FolderPath, ...)`, with `ClaudeCli` reading `.Value` into the argument lists and `WorkingFolder.At`. Verify `ClaudeCliShould` passes.
- [ ] 5.3 Callers drop `.Value`: `LaunchService.cs:27`, `TerminalEffects.cs:18`, `StopSessionHandler.cs:12`, `DeleteSessionHandler.cs:19`, `RenameSessionHandler.cs:26-43`, `ResumeSessionsHandler.cs:28-33`, `AdoptSessionHandler.cs:36-37`, `HereCommandRunner.AttachAsync`; `ClaudeContractShould` passes `FolderPath.From(RepositoryFolder.Root())`. Verify `PortParametersShould.TakeDomainValues` is green with `IGit` the only kept entry, and both test projects pass (the contract facts skip without `VANTAGE_CLAUDE_CONTRACT`).

## 6. `IDesktop.OpenAsync` names its exceptions (after `editor-not-found`)

- [ ] 6.1 Rebase onto `main` with `editor-not-found` merged; confirm `ProgramNotStartedException` and `NoApplicationException` sit beside `IDesktop` and what `OpenSolutionHandler` catches. Verify `dotnet build -c Debug` passes before changing anything.
- [ ] 6.2 `DesktopShould.RefuseToOpenWithoutPowerShell`, `PassTheChildsReasonOn`, `NameTheExitCodeWhenTheChildSaysNothing` and `ThrowWithTheReasonWhenTheFileIsGone` expect `ProgramNotStartedException` with the messages they assert today. Red, then green in `Desktop.OpenAsync` (`:26`, `:35`); `IDesktop.OpenAsync`'s doc names the two exceptions and who catches them, as design.md words it. Verify `DesktopShould` passes.
- [ ] 6.3 `ProblemReportHandlersShould.LeaveTheCrashWhenTheBrowserFails` sets `OpenFailure` to a `ProgramNotStartedException`; `LeaveTheCrashWhenNoBrowserIsAssociated` sets a `NoApplicationException` and expects `NotOpened` too; `LetAnUndeclaredFailureThrough` sets an `InvalidOperationException` and expects it to propagate. Red (the handler catches `InvalidOperationException`), then green in `OpenProblemReportHandler.cs:28`. Verify the facts pass and `OpenPullRequestHandlerShould` and `OpenSolutionHandlerShould` are unchanged.
- [ ] 6.4 Docs: `.claude\rules\security.md`'s programs paragraph says what `IDesktop.OpenAsync` throws and who catches it. Verify by reading against design.md.

## 7. Version

- [ ] 7.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` one preview step above `main` when implemented (patch-level: a refactor). Verify `.github\scripts\Assert-VersionBumped.ps1` passes against `main`.

## 8. Whole-solution check

- [ ] 8.1 `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution, `dotnet format --verify-no-changes` and `openspec validate folder-deleter-and-ports --strict` all pass. Record the totals here.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
- Push and open the implementation pull request only when the owner asks.
