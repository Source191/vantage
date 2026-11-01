# Tasks

Each task is one red-green-refactor cycle with a commit per step; the test is named before the code. The red for each member is its `Kept` entry removed from `NullFreeModelsShould`, so `HoldNoNullableMember` names it, plus the fact named in the task for a new type. Verify each with `dotnet build -c Debug` (warnings are errors) and `dotnet test test\Source191.Vantage.Core.Tests -c Debug --no-build`, adding `test\Source191.Vantage.Infrastructure.Tests` where the task names an Infrastructure test.

## 1. Domain and settings

- [x] 1.1 `SettingsShould` and `SettingsFileShould` resolve the same defaults through `WorktreeRootChoice` and plain values (`Resolve([], new WorktreeRootChoice.BesideFirstRoot(), ..., Settings.BaseWhenUnset, Home)`), and the five `Settings.Resolve` entries leave `Kept`: red, then `Domain\WorktreeRootChoice.cs`, `Settings.Resolve` without nullables and `SettingsFile.From` meeting config.json's missing properties green. Verify every defaulting fact passes with unchanged expectations.
- [x] 1.2 `ToolVersionShould` and `UpdateCheckOptOutShould` lose their null rows and the two entries leave `Kept`: red, then `FromInformational(string)`, `Applies(string)` and `Program.cs`'s `?? string.Empty` green. Verify the empty and blank rows still answer "unknown" and false.

## 2. Application: worktrees and sessions

- [x] 2.1 `WorktreeInspectorShould.AnswerAnUnknownLastCommitWhenGitHasNone` and the counts fact reading `new LastCommit.Made(...)`, with the two `LastCommitAt` entries out of `Kept`: red, then `Application\LastCommit.cs`, `WorktreeCounts`, `WorktreeRow`, `WorktreeInventory`, `WordingOfDetail` and `WorktreesScreenDefinition` green. Verify `WordingOfDetailShould` and `WorktreeInventoryShould` (Infrastructure) pass unchanged in their text.
- [x] 2.2 `SessionActivityShould` reads `new LastPrompt.Seen(...)` and `new LastPrompt.NotSeen()`, with the two `LastPromptedAt` entries out of `Kept`: red, then `Application\LastPrompt.cs`, `SessionActivity`, `SessionDetail` and the pane's Prompted line green. Verify the pane's text facts pass.
- [x] 2.3 `DetailLoaderShould` reads `CreatedByVantage` and `NotByVantage`, with `WorktreeDetail.Created` out of `Kept`: red, then `Application\Detail\WorktreeOrigin.cs`, `DetailLoader` and the pane's Created line green. Verify the pane's text facts pass.
- [x] 2.4 `Attempt.RunAsync(what)` out of `Kept`: red, then the parameter as a `CodeName` green. Verify `AttemptShould` and `DiagnosticsPrivacyShould` pass.

## 3. Application: views and dialogs

- [x] 3.1 `SessionsViewShould` and `WorktreesViewShould` read `new LoadBanner.Failed(...)` and `new LoadBanner.None()`, with the four `Banner` entries out of `Kept`: red, then `Application\Views\LoadBanner.cs`, the three states and `TableBody` green. Verify the view facts pass.
- [x] 3.2 `ListSelectionShould.AnswerItsKeyAsASelectionKey` and `BuildASelectionFromASelectionKey`, with the three selection key entries out of `Kept`: red, then `Application\Views\SelectionKey.cs`, `ListSelection.Selection`, `ListPolicy.List` and the three states green. Verify `WorktreesViewShould`'s selected-row facts pass.
- [x] 3.3 `SettingsViewShould`'s browse facts read `new BrowseStart.At(...)` and `new BrowseStart.NoFolderTyped()`, with `SettingsState.BrowseFrom` out of `Kept`: red, then `Application\Views\BrowseStart.cs`, `SettingsState` and `SettingsDialog` green. Verify the facts pass.
- [x] 3.4 `TabKeysShould` (the five tab keys and a key that is not one) and `TabKeys.For` out of `Kept`: red, then `TabKeys.TryFor` and the three reducers green. Verify the view facts for the tab keys pass.

## 4. Version and the whole build

- [x] 4.1 Raise `Version` in `src\Source191.Vantage\Source191.Vantage.csproj` to `0.19.0-preview.15`. Verify `.github\scripts\Assert-VersionBumped.ps1 -Base v-link-cited-apis -Head HEAD` passes. #150 merged into main while this was under way, so the branch merged `origin/main` and `Assert-VersionBumped.ps1 -Base origin/main -Head HEAD` passes (0.19.0-preview.14 to 0.19.0-preview.15).
- [x] 4.2 Whole-solution check: `dotnet build -c Debug`, `dotnet test -c Debug --no-build` at the solution and `dotnet format --verify-no-changes` pass; `Kept` has 42 entries; headless captures of the three tabs before and after (navigation keys only) read the same; `openspec validate null-free-follow-ups --strict` passes. The smoke check is left to CI's `check` job, because run locally it force-removes an idle worktree under the owner's real roots. Build clean, 2197 tests (2189 passed, 8 skipped), format clean, Kept 42. The ten grids differ only in live data (version, memory, ages, the sessions other agents started meanwhile); one Worktrees capture's Down arrived before a slow scan finished, and a second pair read the same.

## Workflow follow-up

- Archive the change in the pull request that completes it; `skip_specs` means no spec sync.
