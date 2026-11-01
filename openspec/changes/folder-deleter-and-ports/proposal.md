# Proposal

## Why

The code review of 0.20.0-preview.7 (1 November 2026, sections 3.2 and 3.4, item 7 of section 4) found six places where a port's shape lets a rule be broken that `CLAUDE.md` states in words: `IFolders.DeleteTree` reaches 18 types though one calls it (D-08); `IProcessRegistry` is the one boundary interface declared in `Domain`, with nothing stopping a second (D-09); half the ports take `string` where a Domain value exists and half take the value, so the written rule "ports keep strings" matches neither (D-10); `ClaudeExecutable.Locate` is the one PATH search that skips `PathFolders.Searchable` (I-10); `JsonFileStore.RecentFile` compares two paths outside `FolderPath` (I-11); and three callers of `IDesktop.OpenAsync` disagree about what it throws (A-15). Each is a few lines of code and one reflection fact, so they go together as one refactor before go-live.

## What Changes

- **`IFolderDeleter`:** a new port with one member, `void DeleteTree(string path)`, implemented by the same `Adapters\Files\Folders` and the same `FakeFolders`. `IFolders` loses `DeleteTree`. `WorktreeRemover` takes `IFolderDeleter` in place of `IFolders`, and `RemoveWorktreeHandler` and `DeleteSessionHandler` take the one `WorktreeRemover` the composition builds instead of each building its own. A fact in `LayeringShould` says the only types that depend on `IFolderDeleter` are `WorktreeRemover` and its two implementations; it replaces the sentence "a folder is deleted only by `WorktreeRemover`" in `CLAUDE.md` and `.claude\rules\security.md` with a pointer to the fact.
- **`IProcessRegistry` stays in `Domain`, by a fact:** `LayeringShould` gains a fact that `Domain` declares no interface but `ITextValue<T>` and `IProcessRegistry`, with a one-line reason beside the name, as `NullFreeModelsShould.Kept` records its exceptions. design.md says why the alternative (moving it to `Ports` and passing two functions) was not taken.
- **Domain values at the ports:** `CLAUDE.md`'s "ports keep strings and callers pass `.Value`" becomes "a port takes the Domain value where one exists (`SessionId`, `JobId`, `FolderPath`, `BranchName`, `SessionName`); adapters read `.Value`". `IClaudeCli`'s `string directory`, `jobId` and `sessionId` become `FolderPath`, `JobId` and `SessionId`; `ITranscripts.HasMessages` takes a `SessionId`; `IWorktreeIncludeCopier.CopyAsync` takes two `FolderPath`s; `IRecentRepositories` takes and gives `FolderPath`. A new `Architecture\PortParametersShould` fails for a port parameter named `sessionId`, `jobId`, `directory`, `repository`, `worktree` or `branch` that is a `string`, with `IGit` on a `Kept` list with its reason (see Out of scope).
- **Recent repositories keyed by `FolderPath.Key`:** `JsonFileStore` keeps each recent repository under `FolderPath.Key` rather than `ToUpperInvariant()` of the text, so `C:\repos\A\`, `C:/repos/A` and `c:\repos\a` are one entry, as they are one folder to the rest of Vantage. The file written is the same plain string as before.
- **`ClaudeExecutable` searches only searchable PATH folders:** the batch-file search applies `PathFolders.Searchable`, as `ProgramLocator`, `AzureCliExecutable` and `EditorLocator` do, with a fact that a relative PATH entry is not searched for `claude.cmd`.
- **`IDesktop.OpenAsync` names what it throws:** building on the `editor-not-found` change, which adds `NoApplicationException` and `ProgramNotStartedException` beside `IDesktop`, the port declares that `OpenAsync` throws exactly those two, and `Adapters\Processes\Desktop` throws `ProgramNotStartedException` where it throws `InvalidOperationException` today (PowerShell missing, the open script failing). `OpenProblemReportHandler` catches both into `ProblemReportOutcome.NotOpened`; `OpenPullRequestHandler` and `OpenSolutionHandler` let the failures they have no outcome for reach the screen as `Failed:`, which the port's doc now says.
- **Docs:** `CLAUDE.md` (the layers line, the values line, the security line), `.claude\rules\security.md` and `LayeringShould`'s summary say the new facts.
- **Version:** raised one preview step above `main` when implemented; patch-level under semantic versioning, because no key, screen, message, state file or command line behaves differently.
- `skip_specs: true`: a refactor with facts; no spec changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Ports:** new `Ports\IFolderDeleter.cs`; `IFolders`, `IClaudeCli`, `ITranscripts`, `IWorktreeIncludeCopier`, `IRecentRepositories`, `IDesktop` change signatures or docs.
- **Application:** `WorktreeRemover`, `RemoveWorktreeHandler`, `DeleteSessionHandler`, `LaunchService`, `RepositoryCatalogue`, `WizardState`, `OpenProblemReportHandler`, and the `.Value` call sites in `AdoptSessionHandler`, `RenameSessionHandler`, `ResumeSessionsHandler`, `StopSessionHandler`, `CloseEmptyTerminalsHandler`, `SessionRowEnricher`, `TerminalEffects`.
- **Adapters:** `Folders`, `ClaudeCli`, `ClaudeTranscripts`, `WorktreeIncludeCopier`, `JsonFileStore`, `ClaudeExecutable`, `Desktop`.
- **Tui and wiring:** `LaunchWizardDialog`'s recent list type; the lines in `Program.cs` (or `ConsoleComposition` once `tidy-composition-root` lands) that build `Desktop`, `EffectPorts`, `DeleteSessionHandler` and `RemoveWorktreeHandler`.
- **Tests:** `FakeFolders`, `FakeTranscripts`, `InMemoryState`, `FakeDesktop`; `LayeringShould`, new `PortParametersShould`, `ClaudeExecutableShould`, `JsonFileStoreShould`, `DesktopShould`, `ProblemReportHandlersShould`, `ClaudeCliShould`, `ClaudeTranscriptsShould`, `ClaudeContractShould`, and every handler fact that passes a `.Value`.
- **Other open changes:** `listed-name` (the `IClaudeCli` resume overloads), `process-registry-one-path` (`ConfirmedProcess` on `IProcessRegistry`) and `editor-not-found` (the two exceptions) land first; this change rebases onto them, and whichever of this and `tidy-composition-root` lands second rebases its wiring lines.
- **Users:** none.

## Out of scope

- `IGit`'s seventeen string parameters: its own change (`GitCli`, `TemporaryGitRoot` and about forty call sites); until then `PortParametersShould` keeps it on `Kept` with that reason.
- `IFolders`' `string path` parameters: it takes file paths as well as folders, so no one Domain value fits.
- A result record from `IDesktop.OpenAsync`, or new outcome cases for the pull request and solution handlers: design.md says why.
- Moving `IProcessRegistry` to `Ports`: the alternative in design.md.
- Multiple Claude accounts, which is a later change.
