# Tasks

## 1. Domain rules

- [ ] 1.1 `FolderLabelShould.ShowAFolderRelativeToTheRootThatHoldsIt`, `PreferTheDeepestRoot`, `ShowAFolderOutsideEveryRootRelativeToHome`, `ShowAFolderOutsideHomeInFull`, `ShowARootItselfRelativeToHome`: implement `Domain\FolderLabel.For`; verify the facts pass with the spec's Northwind paths

## 2. Application rules

- [ ] 2.1 `SolutionFinderShould.FindTheOnlySolutionInTheFolder`, `LookInSrcWhenTheFolderHasNone`, `PreferTheSolutionNamedAfterTheRepository`, `OfferSeveralWhenNoneIsNamedAfterTheRepository`, `FindNothingWhenNeitherFolderHasOne`, `FindSlnAndSlnx`: add `IFolders.FilesIn` to the port and `FakeFolders`, implement `Application\SolutionFinder` and `SolutionSearch`; verify the facts pass
- [ ] 2.2 `EditorCommandShould.PassTheFolderAsOneArgument`, `ReplaceFolderInsideACustomArgument`, `RefuseABatchFileAsACustomEditor`: implement `Application\EditorCommand.For`; verify a folder holding `&`, `'`, `"` and spaces stays one untouched argument
- [ ] 2.3 `FolderLabelsShould.UseTheRepositoryRootsAndTheWorktreeRoot`, `FollowAChangedConfiguration`: implement `Application\FolderLabels` over the config reader from `settings-and-worktree-root`; verify the facts pass

## 3. Adapters

- [ ] 3.1 `FoldersShould.ListOnlyTheFilesDirectlyInAFolder`: implement `Folders.FilesIn`; verify in `Infrastructure.Tests`
- [ ] 3.2 `WindowsClipboardShould.SetTextThatReadsBackTheSame` (a path with non-ASCII characters, previous clipboard text restored, non-parallel collection): add `IClipboard` and `FakeClipboard`, implement `Adapters\Terminal\WindowsClipboard` with `LibraryImport` and the busy retry; verify the test passes locally and on the runner
- [ ] 3.3 `DesktopShould.StartAProgramWithoutWaitingOrClaudeMarkers` (starts `pwsh -NoProfile -Command` writing its environment to a temporary file, with `CLAUDECODE` set in the parent): add `IDesktop` and `FakeDesktop`, implement `Adapters\Processes\Desktop.StartAsync` and `OpenAsync` (ShellExecute); verify the file shows no `CLAUDECODE` and the call returned before the child exited
- [ ] 3.4 `EditorLocatorShould.FindCodeExeBesideTheCodeCmdShimOnThePath`, `FallBackToTheUserInstallFolder`, `AnswerNullWhenNotInstalled`, `NeverAnswerACmdFile`: add `IEditorLocator` and `FakeEditorLocator`, implement `Adapters\Processes\EditorLocator` for every preset over a temporary folder tree; verify the facts pass

## 4. Copy key

- [ ] 4.1 `SessionsViewShould.CopyTheSelectedSessionsFolder`, `SayNoSessionIsSelectedWhenCopyingFromAnEmptyTable`, `WorktreesViewShould.CopyTheSelectedWorktreesFolder`, `RefuseToCopyAGoneLeftover`: add `CopyFolderPath` and the `c` cases; verify the facts pass
- [ ] 4.2 `CopyFolderPathHandlerShould.ReportThePathCopied`, `ReportABusyClipboard`: implement `Application\Actions\CopyFolderPathHandler` with `FolderPathCopied` and `ClipboardBusy` in their own files, register it in `Composition.Handlers`; verify `EffectDispatcherShould` passes
- [ ] 4.3 `WordingShould` and `WordingOfOutcomesShould` facts for the copy hint, help line and both outcomes; verify the reflection facts find no case without words

## 5. Editor key

- [ ] 5.1 `SessionsViewShould.OpenTheSelectedSessionsFolderInTheEditor` (background, terminal and stopped rows), `WorktreesViewShould.OpenTheSelectedWorktreeInTheEditor`, `RefuseToOpenAGoneLeftover`: add `OpenFolderInEditor` and the `e` cases; verify the facts pass
- [ ] 5.2 `OpenInEditorHandlerShould.StartTheChosenEditorOnTheFolder`, `ReportAnEditorThatIsNotInstalled`, `ReportAMissingFolder`, `RefuseABatchFileEditor`: implement `OpenInEditorHandler` over `IEditorLocator`, `IDesktop` and `IFolders` with `OpenedInEditor`, `EditorNotFound`, `EditorRefused` and `FolderMissing`, register it; verify `EffectDispatcherShould` passes
- [ ] 5.3 `WordingShould` and `WordingOfOutcomesShould` facts for the editor help line and its four outcomes; verify they pass

## 6. Solution key

- [ ] 6.1 `SessionsViewShould.FindTheSelectedSessionsSolution`, `WorktreesViewShould.FindTheSelectedWorktreesSolution`, `RefuseToFindASolutionInAGoneLeftover`: add `FindSolution` (an `IDialogEffect`) and the `v` cases carrying the folder and repository name; verify the facts pass
- [ ] 6.2 `OpenSolutionHandlerShould.OpenTheSolutionWithItsDefaultApplication`: add `OpenSolution`, implement `OpenSolutionHandler` over `IDesktop.OpenAsync` with `OpenedSolution`, register it; verify `EffectDispatcherShould` passes
- [ ] 6.3 `WordingShould` facts for `Footer.NoSolution`, the chooser's title and the help line, and `WordingOfOutcomesShould` for `OpenedSolution`; verify they pass
- [ ] 6.4 Add the `FindSolution` flow to `DialogEffects`: search in the background, dispatch `OpenSolution` for one, a list dialog for several (Esc opens nothing), `Footer.NoSolution` for none; verify `DialogEffectsShould` passes and a headless `drive` pressing `v` on a fixture folder with two solutions captures the chooser

## 7. Screens

- [ ] 7.1 Replace WT with FOLDER in `SessionsScreenDefinition` (columns NAME, STATE, REPOSITORY, FOLDER, BRANCH, AGE, TAB, cells through `FolderLabels`) and use `FolderLabels` for the worktrees FOLDER cell; remove `SessionRow.IsWorktree` and the enricher's flag, updating `SessionRowEnricherShould` and the reducer fixtures; verify `dotnet build -c Debug` and the Core tests pass
- [ ] 7.2 Add the third key-hint column (`<c>` Copy path, `<e>` Editor, `<v>` Solution) to both definitions and `TableScreen`, and the three keys to `Wording.SessionsHelp` and `WorktreesHelp`; verify `WordingShould`'s help facts and `dotnet format --verify-no-changes`
- [ ] 7.3 Wire `WindowsClipboard`, `Desktop`, `EditorLocator` and `FolderLabels` in `Program.cs`; run `dotnet run --project tools\HeadlessTerminal -- check` and a `drive` pressing `c`, `e` and `v` on a session row, and verify the check exits 0, the clipboard holds the path and VS Code opens the folder

## 8. Documentation and release

- [ ] 8.1 README: the sessions column table (FOLDER replaces WT, with the relative-path rule), the `c`, `e` and `v` rows in both key tables, and the editor setting's presets; verify every key the reducers handle appears in the README tables
- [ ] 8.2 CLAUDE.md: `IClipboard`, `IDesktop` and `IEditorLocator` in the layer list, and the rule that folders reach editors as list arguments and never through cmd.exe; verify `openspec validate folder-column-and-open-keys --strict` passes
- [ ] 8.3 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to the next minor after `main`'s; verify `.github\scripts\Assert-VersionBumped.ps1` passes and `dotnet test -c Debug` at the solution is green

## Workflow follow-up

- Merge after `remove-session-descriptions` and `settings-and-worktree-root`; rename `EditorChoice` and the config reader here to whatever those changes settled on.
- Archive the change in the same pull request that completes it, so `openspec\specs` gains `folder-actions` and the modified session-listing requirement.
