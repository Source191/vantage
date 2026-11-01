# Proposal

## Why

The code review of 0.20.0-preview.7 found four ways Vantage's own state files can lose what they hold (I-02, C-04 with I-15, I-14 and D-01), all in `src\Source191.Vantage.Infrastructure\State\JsonFileStore.cs` and the file record it resolves. None needs more than one console under normal use to stay safe, but each is a crash, a second writer or a hand edit away from losing the repository roots or the "created by Vantage" records, and `session-launch` already promises that every record survives two processes writing at once, which `created.json` does not keep.

## What Changes

- **Writes replace the file whole (I-02):** every state file (`config.json`, the tab, recent and created records, `update-check.json`) is written beside its target and moved over it, as `ClaudeSettings.EnableTerminalBellAsync` already writes `settings.json`, so a crash, a kill or a full disc mid-save leaves the previous file rather than a 0-byte `config.json` that reads as every default. Today `JsonFileStore.WriteJsonAsync` (`JsonFileStore.cs:205`) truncates the file in place with `FileMode.Create`; the comment at lines 193 to 196 saying Windows refuses the rename goes, because Vantage's readers open with delete sharing (line 179).
- **One file per created worktree (C-04, I-15):** `created.json`, one list read, changed and written whole by `RecordCreatedWorktreeAsync` and `ForgetCreatedWorktreeAsync` (`JsonFileStore.cs:70-82`), becomes a `created\` folder of one file per worktree, keyed as the recent records are, so a `vantage here` creating a worktree while the console forgets one cannot drop every other record and leave later removals asking about branches Vantage made. The old `created.json` is imported once and removed, as `recent.json` is by `ImportLegacyRecentAsync` (line 115).
- **A file Vantage may not read is absent (I-14):** `ReadJsonAsync` (`JsonFileStore.cs:183-190`) catches `IOException` and `JsonException` but not `UnauthorizedAccessException`, so a state file with a denied ACL throws out of `CurrentSettings.LoadAsync` (`src\Source191.Vantage.Core\Application\CurrentSettings.cs:23`, which catches nothing) and out of start-up at `src\Source191.Vantage\Program.cs:124`. It joins the other two, as `ClaudeSettings.cs:46` already has it.
- **A number where a name belongs cannot crash the Config tab (D-01):** `JsonStringEnumConverter()` (`JsonFileStore.cs:26`) accepts integers, so a hand-edited `"defaultBase": 7` reaches `Settings.Resolve` as `(WorktreeBase)7`, and the first discard-free switch (`src\Source191.Vantage.Core\Application\WordingOfConfig.cs:87-91`, `src\Source191.Vantage.Tui\WizardControls.cs:28-32`) throws `SwitchExpressionException` when the tab draws or the wizard opens. The converter is built with `allowIntegerValues: false`, so an integer fails as an unknown name already does (the file reads as every default), and `SettingsFile.From` (`src\Source191.Vantage.Core\Application\SettingsFile.cs:22,25`) treats an undefined `DefaultBase` or `DefaultPlace` as unset, as line 41 already treats an undefined `EditorPreset`.
- **README:** the "Where Vantage keeps things" table's `created.json` row becomes `created\`, one file each.

Out of scope: a lock or retry around any state file (one file per record removes the need); cleaning up `.tmp` files a crash leaves (nothing reads them); `TryDelete`'s own exceptions (a denied delete should still be reported by the key that asked for it); a per-property fallback for a bad base or place, which would need `config.json`'s enums held as text; the `descriptions\` folder and `sessions.json`, still neither read nor deleted; and multiple Claude accounts, which is a later change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `session-launch`: the "Vantage records what it created" requirement gains scenarios for two processes recording created worktrees at once, a forget during a record, and the one-off import of the old `created.json`.
- `settings`: the "Settings live in one file with defaults" requirement gains scenarios for a number in place of a base or place name and a file Vantage is not allowed to read; a new requirement says a save replaces the file whole, so an interrupted save keeps the previous settings and a reader holding the file open does not stop one.

## Impact

- `Adapters\State\JsonFileStore` (`WriteJsonAsync`, `ReadJsonAsync`, the `created\` folder and its import, the converter's construction, the class summary) and its facts in `test\Source191.Vantage.Infrastructure.Tests\JsonFileStoreShould.cs`, three of which assert `created.json` today (`ReadFilesWrittenByTheCurrentVersion`, `WriteFilesTheCurrentVersionCanRead`, `TreatACreatedFileHoldingARelativePathAsCorrupt`).
- `Application\SettingsFile.From` and `test\Source191.Vantage.Core.Tests\Application\SettingsFileShould.cs`.
- `Ports\ICreatedWorktrees` and `test\Source191.Vantage.TestDoubles\InMemoryState` are unchanged: the port's three methods keep their signatures.
- README's state folder table; `Version` raised in the implementing pull request (patch: fixes only, old state imported, no key changes).
