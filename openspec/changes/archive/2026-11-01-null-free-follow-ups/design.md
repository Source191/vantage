# Design

## Context

See proposal.md for why. As of `v-link-cited-apis` at `a1473d1`, `NullFreeModelsShould.Kept` has 64 entries; 26 were allowed by `null-guard-rule` without a decision. Each is read in a handful of places: `LastCommitAt` by the WORKTREE table's LAST COMMIT cell and the pane's Changes line; `Banner` by `TableBody`'s frame title and waiting spinner; the selection keys only by the states' `List` and `With`; `BrowseFrom` by `SettingsDialog`'s Browse; `TabKeys.For` by the three reducers; `LastPromptedAt` by the session pane's Prompted line; `Created` by the worktree pane's Created line; `Attempt.RunAsync(what)` by three `TableBody` calls, each passing a `CodeName`; `Settings.Resolve` by `SettingsFile.From` and about twenty facts; `ToolVersion.FromInformational` and `UpdateCheckOptOut.Applies` once each in `Program.cs`.

## Goals / Non-Goals

**Goals:**
- The 22 members leave `Kept`, each replaced by a named case or met on the line that receives it, with no new nullable in Domain, Ports or Application.
- Nothing the console shows or decides changes.

**Non-Goals:**
- `ListSelection`'s nullable key and selected row, `TableTitle.Selected`, the states' `Selected`, and `Failed.What`: kept by earlier decisions.
- `ProcessIdentity.StillTheSession` and `TerminalGuiBridge`: kept by the owner.
- Private members and locals, which the fact does not read and which meet the base class library's nullables (`FirstOrDefault`, `TryGetValue`).

## Decisions

**One named type per concept, nested cases written with `new`.** As `Head`, `LiveTab`, `SessionMemory` and `WaitingReason` do. A type has no public `From(T?)`: that would itself be a nullable parameter, so each translation is on the line that receives the null.

**`LastCommit` is `Made(DateTimeOffset At)` or `Unknown`.** `Unknown` rather than `None`, because one case covers git failing, a worktree with no commits, a folder that has gone and the scan's `GitException` arm, all shown as `-` in the table and nothing in the pane today. It lives in `Application` beside `WorktreeCounts`. `WorktreeInspector.CountAsync` translates `IGit.LastCommitAtAsync`'s answer; `WorktreeInventory`'s two rows without counts pass `Unknown`.

**`LoadBanner` is `None` or `Failed(string Message)`.** Named for what it is rather than `Banner`, so the property keeps its name without sharing it with its type. `TableBody` shows the red title for `Failed` and hides the waiting spinner unless `None`, as the null checks did.

**`SelectionKey` is `None` or `Row(string Key)`, translated in `ListSelection` and `ListPolicy`.** The owner kept `ListSelection.SelectedKey` nullable, so the translation runs both ways where a state copies the selection: `ListSelection.Selection` answers its key as a `SelectionKey` (for `With`), and `ListPolicy.List(rows, key, filter, editing, loaded)` builds a selection from one (for `List`). Putting both on the selection's own types keeps one translation for three states. Alternative: empty text as the "no key" marker (rejected: meaning in a magic value, which is what the programme removes).

**`BrowseStart` is `NoFolderTyped` or `At(FolderPath Folder)`.** `NoFolderTyped` rather than `Default`, which CA1716 refuses as a keyword. The private `FolderTyped()` keeps its `FolderPath?` from `FirstOrDefault`, and `BrowseFrom` translates it with the `Parent ?? typed` rule on one line.

**`TabKeys.TryFor` replaces `For`.** A Try method's `out` marked `[NotNullWhen(true)]` is a house shape, as `KeyMapping.TryFrom` is, and reads naturally in each reducer's `when` guard. A new `TabKeysShould` pins the five keys and a key that is not one.

**`LastPrompt` is `Seen(DateTimeOffset At)` or `NotSeen`, and `WorktreeOrigin` is `CreatedByVantage(CreatedWorktree Record)` or `NotByVantage`.** `SessionActivity.LastPromptedAt` translates on its `TryGetValue` line; `DetailLoader`'s record lookup answers `NotByVantage` both when no record matches and when reading the records fails, as the pane reads "not by Vantage" for both today.

**`Attempt.RunAsync` takes a `CodeName`.** Every caller passes one, so no overload for a call without one (YAGNI); the diagnostic event `Failed` keeps its optional `What` for `CrashRecorder`, whose failures have no effect to name.

**`Settings.Resolve` takes no nullable.** The roots are a list, where empty means `<home>\source\repos` as today; the worktree root is a `WorktreeRootChoice` (`BesideFirstRoot`, `At(FolderPath Folder)`, in `Domain` because the rule that places it beside the first root is the Domain's); the base, update check and place are plain values. Their defaults are named once on `Settings` (`BaseWhenUnset`, `ChecksForUpdatesWhenUnset`, `PlaceWhenUnset`), which `Resolve`'s optional parameters and `SettingsFile`'s `??` both read, so the defaults keep one home. `SettingsFile.From` is the line that meets config.json.

**`ToolVersion.FromInformational(string)` and `UpdateCheckOptOut.Applies(string)`.** Both already treat empty text as the absence (unknown, and the check left to the setting), so `Program.cs` adds `?? string.Empty` to the line that reads the attribute and the variable. The facts keep their empty and blank rows and drop the null rows.

**The four entries that stay keep their reasons.** `Kept` does not mark candidates apart from decided entries, so `ProcessIdentity.StillTheSession` and `TerminalGuiBridge`'s three need no rewording.

## Risks / Trade-offs

- [A pane line or cell changes text] → the `WordingOfDetailShould` and screen facts keep their expected text; headless captures of the three tabs before and after (navigation keys only) read the same.
- [A default changes] → `SettingsShould` and `SettingsFileShould.ResolveAMissingConfigToTheDefaults` keep their expectations.
- [The base branch moves] → `v-link-cited-apis` is being merged with main; the branch merges it before the pull request and runs every check again.
