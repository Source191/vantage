# Proposal

## Why

`null-guard-rule` (#148) added `Architecture\NullFreeModelsShould`, whose `Kept` list allowed 26 nullables that no change of the null programme had taken, as "Candidates for a follow-up change". The owner decided on 1 November 2026 to fix everything possible: 15 of them are Vantage's own meaning and become named cases, and 7 are absences from outside Vantage that move to the line receiving them. The other 4 stay, as the owner chose: `ProcessIdentity.StillTheSession(DateTimeOffset?)` (kept on 31 October) and `TerminalGuiBridge`'s three `ILogger` signatures.

## What Changes

- **`WorktreeCounts.LastCommitAt` and `WorktreeRow.LastCommitAt`**: a `LastCommit` (`Made(DateTimeOffset At)`, `Unknown`), translated from `IGit.LastCommitAtAsync`'s answer in `WorktreeInspector.CountAsync`; `IGit` keeps its nullable query.
- **`Banner`** on `IListViewState<TRow>` and the Sessions, Worktrees and Config view states: a `LoadBanner` (`None`, `Failed(string Message)`).
- **The selection keys** `SessionsViewState.SelectedSessionId`, `WorktreesViewState.SelectedPath` and `ConfigViewState.SelectedKey`: a `SelectionKey` (`None`, `Row(string Key)`). `ListSelection.SelectedKey`, `ListSelection.Selected`, `TableTitle.Selected` and the states' `Selected` stay nullable, as the owner decided earlier; `ListSelection` gains `Selection` (its key as a `SelectionKey`) and `ListPolicy` gains `List(...)` (a selection from one), the two lines where a state copies the selection.
- **`SettingsState.BrowseFrom`**: a `BrowseStart` (`NoFolderTyped`, `At(FolderPath Folder)`); `SettingsDialog` sets the picker's path only for `At`.
- **`TabKeys.For`**: `TabKeys.TryFor(Key, [NotNullWhen(true)] out ViewEffect? effect)`, the house's Try shape.
- **`SessionActivity.LastPromptedAt` and `SessionDetail.LastPromptedAt`**: a `LastPrompt` (`Seen(DateTimeOffset At)`, `NotSeen`).
- **`WorktreeDetail.Created`**: a `WorktreeOrigin` (`CreatedByVantage(CreatedWorktree Record)`, `NotByVantage`).
- **`Attempt.RunAsync(what)`**: a `CodeName`, since every caller passes one; `Failed.What` stays nullable (kept by `null-guard-rule` for failures outside an effect).
- **`Settings.Resolve`**: no nullable parameter. The roots are a list (empty still means the default), the worktree root a `WorktreeRootChoice` (`BesideFirstRoot`, `At(FolderPath Folder)`), and the base, update check and place plain values whose defaults `Settings` names once (`BaseWhenUnset`, `ChecksForUpdatesWhenUnset`, `PlaceWhenUnset`); `SettingsFile` meets config.json's missing properties.
- **`ToolVersion.FromInformational(string)`** and **`UpdateCheckOptOut.Applies(string)`**: `Program.cs` translates the missing assembly attribute and the unset environment variable to empty text on the line that reads them, which both already treat as their absence.
- Every displayed text, detail pane line, setting default and update decision stays as it is. `Kept` goes from 64 entries to 42.
- **Version:** `0.19.0-preview.15`, one step above the base branch.
- `skip_specs: true`: a refactor with no change in behaviour.

Out of scope: the 42 entries that stay on `Kept` (the `IGit`, `IProcessRegistry` and JSON shapes, the list selection, `WizardState.SelectedIndex`, the diagnostic events' optional members, `ProcessIdentity.StillTheSession` and `TerminalGuiBridge`); private and local nullables; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None: a refactor with no change in behaviour, so `.openspec.yaml` declares `skip_specs: true`.

## Impact

- **Code:** new `Application\LastCommit.cs`, `Application\LastPrompt.cs`, `Application\Detail\WorktreeOrigin.cs`, `Application\Views\LoadBanner.cs`, `Application\Views\SelectionKey.cs`, `Application\Views\BrowseStart.cs`, `Domain\WorktreeRootChoice.cs`; `Application\WorktreeInspector.cs`, `Application\WorktreeInventory.cs`, `Application\Views\WorktreeRow.cs`, `Application\WordingOfDetail.cs`, `Application\SessionActivity.cs`, `Application\Detail\Detail.cs`, `Application\Detail\DetailLoader.cs`, `Application\Views\IListViewState.cs`, `ListSelection.cs`, `SessionsView.cs`, `WorktreesView.cs`, `ConfigView.cs`, `ConsoleView.cs` (`TabKeys`), `SettingsState.cs`, `Application\Diagnostics\Attempt.cs`, `Application\SettingsFile.cs`, `Domain\Settings.cs`, `Domain\ToolVersion.cs`, `Domain\UpdateCheckOptOut.cs`; `Tui\TableBody.cs`, `Tui\WorktreesScreenDefinition.cs`, `Tui\SettingsDialog.cs`; `Program.cs`.
- **Tests:** `NullFreeModelsShould` (22 entries out of `Kept`), new `TabKeysShould`, and the facts that build or read the changed members (`WorktreeInspectorShould`, `WorktreeInventoryShould`, `SessionActivityShould`, `DetailLoaderShould`, `WordingOfDetailShould`, the three view facts, `SettingsViewShould`, `SettingsShould`, `SettingsFileShould`, `ToolVersionShould`, `UpdateCheckOptOutShould` and the `Settings.Resolve` callers), `TestDoubles\TestSettings`.
- **Docs:** `src\Source191.Vantage\Source191.Vantage.csproj` (`Version`).

## Order

The follow-up to the null programme's ten changes, stacked on `link-cited-apis` (#150).
