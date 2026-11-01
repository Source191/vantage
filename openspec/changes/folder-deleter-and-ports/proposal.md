# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026, sections 3.2 and 3.4, item 7 of section 4) found six places where a port's shape lets a rule be broken that `CLAUDE.md` states in words: `IFolders.DeleteTree` reaches 18 types though one calls it (D-08); `IProcessRegistry` is the one boundary interface declared in `Domain`, with nothing stopping a second (D-09); half the ports take `string` where a Domain value exists and half take the value, so the written rule "ports keep strings" matches neither (D-10); `ClaudeExecutable.Locate` is the one PATH search that skips `PathFolders.Searchable` (I-10); `JsonFileStore.RecentFile` compares two paths outside `FolderPath` (I-11); and the callers of `IDesktop.OpenAsync` (three at the review, four since `show-release-notes`, PR #180) disagree about what it throws (A-15). Each is a few lines of code and one reflection fact, so they go together as one refactor before go-live. This proposal reads against `main` 8dbf4150 (0.20.0-preview.13), after `tidy-composition-root` (PR #177), `editor-not-found` (PR #178), `performance-diagnostics` (PR #179) and `show-release-notes` (PR #180) landed.

## What Changes

- **`IFolderDeleter`:** a new port with one member, `void DeleteTree(string path)`, implemented by the same `Adapters\Files\Folders` and the same `FakeFolders`. `IFolders` loses `DeleteTree`. `WorktreeRemover` takes `IFolderDeleter` in place of `IFolders`, and `RemoveWorktreeHandler` and `DeleteSessionHandler` take the one `WorktreeRemover` that `Composition\ConsoleComposition.Dialogs` builds instead of each building its own. A fact in `LayeringShould` says the only types that depend on `IFolderDeleter` are `WorktreeRemover` and its two implementations; it replaces the sentence "a folder is deleted only by `WorktreeRemover`" in `CLAUDE.md` and `.claude\rules\security.md` with a pointer to the fact.
- **`IProcessRegistry` stays in `Domain`, by a fact:** `LayeringShould` gains a fact that `Domain` declares no interface but `ITextValue<T>` and `IProcessRegistry`, with a one-line reason beside the name, as `NullFreeModelsShould.Kept` records its exceptions. design.md says why the alternative (moving it to `Ports` and passing two functions) was not taken.
- **Domain values at the ports:** `CLAUDE.md`'s "ports keep strings and callers pass `.Value`" becomes "a port takes the Domain value where one exists (`SessionId`, `JobId`, `FolderPath`, `BranchName`, `SessionName`); adapters read `.Value`". `IClaudeCli`'s `string directory`, `jobId` and `sessionId` become `FolderPath`, `JobId` and `SessionId`; `ITranscripts.HasMessages` takes a `SessionId`; `IWorktreeIncludeCopier.CopyAsync` takes two `FolderPath`s; `IRecentRepositories` takes and gives `FolderPath`. A new `Architecture\PortParametersShould` fails for a port parameter named `sessionId`, `jobId`, `directory`, `repository`, `worktree` or `branch` that is a `string`, with `IGit` on a `Kept` list with its reason (see Out of scope). The ports PR #180 added (`IPackagedReleaseNotes`, `ILastRunVersion`, the `IPackageVersions` members) take no such string, so the fact finds nothing in them.
- **Recent repositories keyed by `FolderPath.Key`:** `JsonFileStore` keeps each recent repository under `FolderPath.Key` rather than `ToUpperInvariant()` of the text, so `C:\repos\A\`, `C:/repos/A` and `c:\repos\a` are one entry, as they are one folder to the rest of Vantage. The file written is the same plain string as before.
- **`ClaudeExecutable` searches only searchable PATH folders:** the batch-file search applies `PathFolders.Searchable`, as `ProgramLocator`, `AzureCliExecutable` and `EditorLocator` do, with a fact that a relative PATH entry is not searched for `claude.cmd`.
- **`IDesktop.OpenAsync` names what it throws:** `editor-not-found` (PR #178) put `NoApplicationException` (thrown by `OpenAsync` for a file whose type has no application) and `ProgramNotStartedException` (thrown by `StartAsync`) beside `IDesktop`, and `OpenAsync`'s doc still ends "and otherwise, with the reason, when nothing could be opened". This change makes the port declare that `OpenAsync` throws exactly those two, and `Adapters\Processes\Desktop` throws `ProgramNotStartedException` where it throws `InvalidOperationException` today (PowerShell missing, the open script failing). The two callers that open a URL and have a `NotOpened` outcome, `OpenProblemReportHandler` and `OpenReleasePageHandler` (PR #180's, the fourth caller), catch `ProgramNotStartedException` into it; `OpenPullRequestHandler` and `OpenSolutionHandler` let the failures they have no outcome for reach the screen as `Failed:`, which the port's doc now says.
- **Docs:** `CLAUDE.md` (the layers line, the values line, the security line), `.claude\rules\security.md` and `LayeringShould`'s summary say the new facts.
- **Version:** raised one preview step above `main` when implemented (0.20.0-preview.14 if nothing lands first); patch-level under semantic versioning, because no key, screen, message, state file or command line behaves differently; with one line under Unreleased in `CHANGELOG.md` (under Internal), which `Assert-ChangelogUpdated.ps1` requires.
- `skip_specs: true`: a refactor with facts; no spec changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Ports:** new `Ports\IFolderDeleter.cs`; `IFolders`, `IClaudeCli`, `ITranscripts`, `IWorktreeIncludeCopier`, `IRecentRepositories`, `IDesktop` and `ProgramNotStartedException`'s summary change signatures or docs.
- **Application:** `WorktreeRemover`, `RemoveWorktreeHandler`, `DeleteSessionHandler`, `LaunchService`, `RepositoryCatalogue`, `WizardState`, `OpenProblemReportHandler`, `OpenReleasePageHandler`, and the `.Value` call sites in `AdoptSessionHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`, `StopSessionHandler`, `CloseEmptyTerminalsHandler`, `SessionRowEnricher`, `Actions\TerminalEffects`.
- **Adapters:** `Folders`, `ClaudeCli`, `ClaudeTranscripts`, `WorktreeIncludeCopier`, `JsonFileStore`, `ClaudeExecutable`, `Desktop`.
- **Tui and wiring:** `LaunchWizardDialog`'s recent list type; in `src\Source191.Vantage\Composition\ConsoleComposition.cs` the lines that build `Desktop` and `EffectPorts` (`:79`, `:81-83`, unchanged in shape) and the two handlers for `DialogEffects` (`:95-96`, which gain the shared `WorktreeRemover`). `LaunchParts.cs:20` (`LaunchService` over `new WorktreeIncludeCopier(git)`) and `SharedParts.cs:29` (`JsonFileStore`) are unchanged.
- **Tests:** `FakeFolders`, `FakeTranscripts`, `InMemoryState`, `FakeDesktop`; `LayeringShould`, new `PortParametersShould`, `Application\ClaudeExecutableShould`, `JsonFileStoreShould`, `DesktopShould`, `ProblemReportHandlersShould`, `OpenReleasePageHandlerShould`, `ClaudeCliShould`, `ClaudeTranscriptsShould`, `Contract\ClaudeContractShould`, and every handler fact that passes a `.Value`.
- **Other changes:** `tidy-composition-root` (PR #177) and `editor-not-found` (PR #178) have landed, and this design reads against them. `listed-name` (the `IClaudeCli` resume overloads) and `process-registry-one-path` (`ConfirmedProcess` on `IProcessRegistry`) are still proposals and land first; this change rebases its `IClaudeCli` tasks onto them.
- **Users:** none.

## Out of scope

- `IGit`'s seventeen methods and their twenty-four string parameters (`repository`, `worktree`, `branch` and `directory` among them, plus `path`, `baseRef` and `into`, which the fact does not name): its own change (`GitCli`, `TemporaryGitRoot` and about forty call sites); until then `PortParametersShould` keeps the whole interface on `Kept` with that reason.
- `IFolders`' `string path` parameters: it takes file paths as well as folders, so no one Domain value fits.
- A result record from `IDesktop.OpenAsync`, or new outcome cases for the pull request and solution handlers: design.md says why.
- Moving `IProcessRegistry` to `Ports`: the alternative in design.md.
- Multiple Claude accounts, which is a later change.
